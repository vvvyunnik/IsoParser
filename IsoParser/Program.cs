using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text.RegularExpressions;

namespace IsoParser
{
    class Program
    {
        static void Main(string[] args)
        {
            //OBJECTS TO TEST
            //var countryCodes = new List<string> { "BY", "US" };
            //var countryLanguageCodes = new Dictionary<string, string>
            //{
            //    { "BY", "ru" }
            //};

            var countryCodes = new List<string> { "US", "AL", "DZ", "AS", "AD", "AO", "AI", "AQ", "AG", "AR", "AM", "AW", "AU", "AT", "AZ", "BS", "BH", "BD", "BB", "BE", "BZ", "BJ", "BM", "BT", "BO", "BW", "BV", "BR", "IO", "BG", "BF", "CM", "CA", "CV", "KY", "CF", "TD", "CL", "CN", "CX", "CC", "CO", "KM", "CG", "CK", "CR", "CI", "HR", "CU", "CY", "CZ", "DK", "DJ", "DM", "DO", "TL", "EC", "EG", "SV", "GQ", "ER", "EE", "ET", "FK", "FO", "FJ", "FI", "FR", "FX", "GF", "PF", "TF", "MK", "GA", "GM", "GE", "DE", "GH", "GI", "GR", "GL", "GD", "GP", "GU", "GT", "GN", "GW", "GY", "HT", "HM", "HN", "HK", "HU", "IS", "IN", "ID", "IR", "IQ", "IE", "IL", "IT", "JM", "JP", "JO", "KZ", "KE", "KI", "KP", "KR", "KW", "KG", "LA", "LV", "LB", "LS", "LR", "LY", "LI", "LT", "LU", "MO", "MG", "MW", "MY", "MV", "ML", "MT", "MH", "MQ", "MR", "MU", "YT", "MX", "FM", "MD", "MC", "MN", "MS", "MA", "MZ", "MM", "NA", "NR", "NP", "NL", "AN", "NC", "NZ", "NI", "NE", "NG", "NU", "NF", "MP", "NO", "OM", "PK", "PW", "PA", "PG", "PY", "PE", "PH", "PN", "PL", "PT", "PR", "QA", "RE", "RO", "RU", "RW", "SH", "KN", "LC", "PM", "VC", "WS", "SM", "ST", "SA", "SN", "SC", "SL", "SG", "SK", "SI", "SB", "SO", "ZA", "GS", "ES", "LK", "SD", "SR", "SJ", "SZ", "SE", "CH", "SY", "TW", "TJ", "TZ", "TH", "TG", "TK", "TO", "TT", "TN", "TR", "TM", "TC", "TV", "UG", "UA", "AE", "UM", "UY", "UZ", "VU", "VA", "VE", "VN", "VG", "VI", "WF", "EH", "YE", "YU", "ZR", "ZM", "ZW", "RS", "ME", "CD", "CW", "GG", "IM", "JE", "PS", "MF", "SX", "BL", "SS", "BQ" };            
            const string filepath = @"C:\work\counties.csv";
            var csvLines = new List<string>();
            var header = "CountyCode,CountyName,ParentCountyCode,CountryCode";
            csvLines.Add(header);

            var counties = GetCSV<List<string>>(countryCodes);

            const string jsonFile = @"countries.json";
            var json = File.ReadAllText(jsonFile);
            var countryLanguageCodes = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            var certainCounties = GetCSV<IDictionary<string, string>>(countryLanguageCodes);

            csvLines.AddRange(counties);
            csvLines.AddRange(certainCounties);
            File.WriteAllLines(filepath, csvLines);
            Console.WriteLine("Work Is Completed Succesffully");
        }

        static List<string> GetCSV<T>(ICollection inputCollection)
        {
            const string separator = ",";
            var csvLines = new List<string>();

            //FOR COUNTRIES FROM JSON
            if (inputCollection is IDictionary<string, string>)
            {
                var dict = (IDictionary<string, string>)inputCollection;

                foreach (var countryInfo in dict)
                {
                    var counties = GetCounty(countryInfo.Key, countryInfo.Value);

                    if (counties == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < counties.Count; i++)
                    {
                        counties[i].Add(countryInfo.Key);
                        csvLines.Add(string.Join(separator, counties[i]));
                    }
                }
            }

            //FOR COUNTRIES FROM ARRAY
            if (inputCollection is List<string>)
            {
                var dict = (List<string>)inputCollection;

                foreach (var countryInfo in dict)
                {
                    var counties = GetCounty(countryInfo);

                    if (counties == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < counties.Count; i++)
                    {
                        counties[i].Add(countryInfo.ToString());
                        csvLines.Add(string.Join(separator, counties[i]));
                    }
                }
            }

            return csvLines;
        }

        static List<List<string>> GetCounty(string countryCode, string usedLanguageCode = "")
        {
            string url = $"https://www.iso.org/obp/ui#iso:code:3166:{countryCode}";
            var driver = new ChromeDriver
            {
                Url = url
            };
            var waitDriver = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            var isTable = waitDriver.Until((driver) =>
            {
                return driver.FindElement(By.Id("subdivision")).Displayed;
            });

            if (!isTable)
            {
                return null;
            }

            var table = driver.FindElement(By.Id("subdivision"));
            //CLICK TO SORT BY CODE LANGUAGE
            var sortBtn = table.FindElements(By.ClassName("header"))[4];
            sortBtn.Click();

            var arrow = driver.FindElements(By.ClassName("headerSortDown"));

            if (arrow == null)
            {
                return null;
            }         

            var tableBody = table.FindElement(By.TagName("tbody"));
            var tableRows = tableBody.FindElements(By.TagName("tr"));
            var tableCells = tableRows.Select(x => x.FindElements(By.TagName("td")));

            var lines = new List<List<string>>();
            var languageCodes = new Dictionary<int, string>();

            foreach (var rowCells in tableCells)
            {
                var languageCode = rowCells[4].Text;
                var countyCode = rowCells[1].Text.Replace("*", "");
                var subDivision = rowCells[2].Text;
                var parentSubdivision = rowCells[6].Text;

                var line = new List<string>
                {
                    countyCode
                };

                if (subDivision.Contains("(see also"))
                {
                    //if string contains (see also separate country code entry under COUNTRY_CODE - trim it
                    subDivision = Regex.Replace(subDivision, @"\s\(see also[^)]*\)", "");
                }

                if (subDivision.Contains(","))
                {
                    line.Add($"\"{subDivision}\"");
                }
                else
                {
                    line.Add(subDivision);
                }

                if (string.IsNullOrWhiteSpace(parentSubdivision))
                {
                    line.Add(" ");
                }
                else
                {
                    line.Add(parentSubdivision);
                }


                lines.Add(line);
                languageCodes.Add(lines.IndexOf(line), languageCode);
            }

            driver.Quit();

            var noDuplicateLines = lines.GroupBy(x => x[0]).Select(x => x.FirstOrDefault()).ToList();

            if (lines.Count != noDuplicateLines.Count && languageCodes.Count != 0)
            {
                var filepath = @"C:\work\CountryWithCountyCodeDuplicates.txt";
                SaveTextFile(filepath, countryCode);

                if (!string.IsNullOrWhiteSpace(usedLanguageCode))
                {
                    var indexes = languageCodes.Where(x => x.Value.Equals(usedLanguageCode)).Select(x => x.Key).ToArray();
                    var result = new List<List<string>>();

                    foreach (var index in indexes)
                    {
                        result.Add(lines[index]);
                    }

                    return result.GroupBy(x => x[0]).Select(x => x.FirstOrDefault()).ToList();
                }
            }

            if (lines.Count == noDuplicateLines.Count && languageCodes.Count != 0)
            {
                var filepath = @"C:\work\countries.txt";
                SaveTextFile(filepath, countryCode);

                return lines;
            }

            if (languageCodes.Count == 0)
            {
                var filepath = @"C:\work\NoCountryRegion.txt";
                SaveTextFile(filepath, countryCode);

                return null;
            }

            return null;
        }

        static void SaveTextFile(string filepath, string content)
        {
            using StreamWriter sw = File.AppendText(filepath);
            sw.WriteLine(content);
        }
    }
}
