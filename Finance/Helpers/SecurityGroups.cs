using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Finance.Helpers;
using System.Reflection;
using System.IO;

namespace Finance
{

    public class SecurityGroup
    {
        public SecurityGroupName SecurityGroupName { get; }
        public List<string> Tickers { get; set; }

        private SecurityGroup(SecurityGroupName securityGroupName)
        {
            SecurityGroupName = securityGroupName;
        }

        public static SecurityGroup GetGroup(SecurityGroupName securityGroupName)
        {
            var ret = new SecurityGroup(securityGroupName);
            ret.Tickers = GetTickers(securityGroupName);
            return ret;
        }
        private static List<string> GetTickers(SecurityGroupName securityGroupName)
        {
            var ret = new List<string>();
            string fileName = String.Empty;

            var value = securityGroupName.GetType().GetMember(securityGroupName.ToString());
            if (Attribute.IsDefined(value.FirstOrDefault(), typeof(FileNameAttribute)))
                fileName = value[0].GetCustomAttribute<FileNameAttribute>().FileName;

            if (File.Exists($@".\Resources\DowJonesSecurities.txt"))
            {
                Console.WriteLine("Found it");
                using (var streamReader = new StreamReader($@".\Resources\DowJonesSecurities.txt"))
                {
                    while (!streamReader.EndOfStream)
                        ret.Add(streamReader.ReadLine());
                    streamReader.Close();
                    return ret;
                }
            }
            return null;
        }
    }

}
