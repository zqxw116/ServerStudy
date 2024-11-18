using System.Xml;

namespace PacketGenerator
{
    /// <summary>
    /// 패킷을 생성하는 클래스
    /// 패킷 정의를 어떻게 할 것인지
    /// </summary>
    internal class Program
    {
        static string genPackets;

        static ushort packetId;
        static string packetEnums;
        static void Main(string[] args)
        {
            XmlReaderSettings settings = new XmlReaderSettings()
            {
                IgnoreComments = true,
                IgnoreWhitespace = true,
            };
            using (XmlReader r = XmlReader.Create("PDL.xml", settings))
            {
                r.MoveToContent();
                // string형식으로 한줄 한줄 읽어온다.
                while (r.Read())
                {
                    if (r.Depth == 1 && r.NodeType == XmlNodeType.Element)
                    {
                        ParsePacket(r);
                    }
                    //Console.WriteLine(r.Name + " " + r["name"]);
                }
            }
            string fileText = string.Format(PacketFormat.fileFormat, packetEnums, genPackets);

            File.WriteAllText("GenPackets.cs", fileText);

        }

        public static void ParsePacket(XmlReader _r)
        {
            if (_r.NodeType == XmlNodeType.EndElement)
                return;

            if (_r.Name.ToLower() != "packet")
            {
                Console.WriteLine("[PacketGenerator] invalid packet node");
                return;
            }

            string packetName = _r["name"];
            if (string.IsNullOrEmpty(packetName))
            {
                Console.WriteLine("[PacketGenerator] Packet without name");
                return;
            }

            Tuple<string, string, string> t = ParseMembers(_r);
            genPackets += string.Format(PacketFormat.packetFormat, packetName, t.Item1, t.Item2, t.Item3);
            packetEnums += string.Format(PacketFormat.packetEnumFormat, packetName, ++packetId) + Environment.NewLine + "\t";
        }

        // {1} 멤버 변수들
        // {2} 멤버 변수 Read
        // {3} 멤버 변수 Write
        public static Tuple<string, string, string> ParseMembers(XmlReader _r)
        {
            string packetName = _r["name"];
            string memberCode = "";
            string readCode = "";
            string writeCode = "";

            int depth = _r.Depth + 1; // 파싱하려는 애들의 정보. long playerId부터
            while (_r.Read())
            {
                if (_r.Depth != depth)
                {
                    break;
                }

                string memberName = _r["name"];
                if (string.IsNullOrEmpty(memberName))
                {
                    Console.WriteLine("[PacketGenerator] Member without name");
                    return null;
                }

                if (string.IsNullOrEmpty(memberCode) == false)
                    memberCode += Environment.NewLine; // 엔터를 한 것
                if (string.IsNullOrEmpty(readCode) == false)
                    readCode += Environment.NewLine; // 엔터를 한 것
                if (string.IsNullOrEmpty(writeCode) == false)
                    writeCode += Environment.NewLine; // 엔터를 한 것


                string memberType = _r.Name.ToLower(); // 혹시나 실수 한게 있을지도 모르니까 소문자로
                switch (memberType)
                {
                    case "byte":
                    case "sbyte":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.readByteFormat, memberName,  memberType);
                        writeCode += string.Format(PacketFormat.writeByteFormat, memberName, memberType);
                        break;
                    case "bool":
                    case "short":
                    case "ushort":
                    case "int":
                    case "long":
                    case "float":
                    case "double":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.readForamt, memberName, ToMemberType(memberType), memberType);
                        writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
                        break;
                    case "string":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.readStringFormat, memberName);
                        writeCode += string.Format(PacketFormat.writeStringFormat, memberName);
                        break;
                    case "list":
                        Tuple<string, string, string> t= ParseList(_r);
                        memberCode += t.Item1;
                        readCode += t.Item2;
                        writeCode += t.Item3;
                        break;
                    default:
                        break;
                }
            }
            // 엔터가 있으면 텝도 해라
            memberCode = memberCode.Replace("\n", "\n\t");
            readCode = readCode.Replace("\n", "\n\t\t");
            writeCode = writeCode.Replace("\n", "\n\t\t");
            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
        }

        public static Tuple<string, string, string> ParseList(XmlReader _r)
        {
            string listName = _r["name"];
            if (string.IsNullOrEmpty(listName))
            {
                Console.WriteLine("[PacketGenerator] List without name");
                return null;
            }
            Tuple<string, string, string> t = ParseMembers(_r);
            string memberCode = string.Format(PacketFormat.memberListFormat,
                FirstCharToUpper(listName),
                FirstCharToLower(listName),
                t.Item1,
                t.Item2,
                t.Item3);

            string readCode = string.Format(PacketFormat.readListFormat,
                FirstCharToUpper(listName),
                FirstCharToLower(listName));
            string writeCode = string.Format(PacketFormat.writeListFormat,
                FirstCharToUpper(listName),
                FirstCharToLower(listName));

            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
        }


        public static string ToMemberType(string _memberType)
        {
            switch (_memberType)
            {
                case "bool":
                    return "ToBoolean";
                case "short":
                    return "ToInt16";
                case "ushort":
                    return "ToUInt16";
                case "int":
                    return "ToInt32";
                case "long":
                    return "ToInt64";
                case "float":
                    return "ToSingle";
                case "double":
                    return "ToDouble";
                default:
                    return "";
            }
        }

        public static string FirstCharToUpper(string _input)
        {
            if (string.IsNullOrEmpty(_input))
            {
                return "";
            }
            return _input[0].ToString().ToUpper() + _input.Substring(1);
        }

        public static string FirstCharToLower(string _input)
        {
            if (string.IsNullOrEmpty(_input))
            {
                return "";
            }
            return _input[0].ToString().ToLower() + _input.Substring(1);
        }
    }
}
