using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.IO;
using System.Net;

namespace OTG
{
    struct UpdateData
    {
        public string url;
        public string manifest;
        public DateTime date;
    }

    struct FileData
    {
        public string name;
        public string url;
        public DateTime date;
    }

    class Program
    {
        static List<UpdateData> ExtractData(string input)
        {
            List<UpdateData> output = new List<UpdateData>();

            int table_begin = input.IndexOf("<table>");
            int table_end = input.IndexOf("</table>");

            int current_position = table_begin;

            while (current_position < table_end)
            {
                UpdateData new_data;

                {
                    // get the image

                    current_position = input.IndexOf("<tr>", current_position);

                    current_position = input.IndexOf("<img src=", current_position);

                    if (current_position == -1) break;

                    int image_begin = input.IndexOf("\"", current_position) + 1;
                    int image_end = input.IndexOf("\"", image_begin + 1);

                    string image = input.Substring(image_begin, image_end - image_begin);

                    current_position = input.IndexOf("</td>", image_end);

                    if (image.Contains("folder.gif") == false) continue;

                }

                {
                    // get the manifest id

                    current_position = input.IndexOf("<td>", current_position);
                    current_position = input.IndexOf("<a href=", current_position);

                    int manifest_begin = input.IndexOf("\"", current_position) + 1;
                    int manifest_end = input.IndexOf("/\"", manifest_begin + 1);

                    new_data.manifest = input.Substring(manifest_begin, manifest_end - manifest_begin);

                    new_data.url = "http://osw.didrole.com/src/" + new_data.manifest + "/";

                    current_position = input.IndexOf("</td>", current_position);
                }

                {
                    // get the date
                    current_position = input.IndexOf("<td align=\"right\"", current_position);
                    int date_begin = input.IndexOf(">", current_position) + 1;
                    int date_end = input.IndexOf("  </td>", current_position);

                    string date_string = input.Substring(date_begin, date_end - date_begin);

                    new_data.date = DateTime.Parse(date_string);

                    current_position = input.IndexOf("</td>", current_position);
                }

                output.Add(new_data);
            }

            return output;
        }

        static List<FileData> ExtractFileData(string input)
        {
            List<FileData> output = new List<FileData>();

            int table_begin = input.IndexOf("<table>");
            int table_end = input.IndexOf("</table>");

            int current_position = table_begin;

            while (current_position < table_end)
            {
                FileData new_data;

                {
                    // get the image

                    current_position = input.IndexOf("<tr>", current_position);

                    current_position = input.IndexOf("<img src=", current_position);

                    if (current_position == -1) break;

                    int image_begin = input.IndexOf("\"", current_position) + 1;
                    int image_end = input.IndexOf("\"", image_begin + 1);

                    string image = input.Substring(image_begin, image_end - image_begin);

                    current_position = input.IndexOf("</td>", image_end);

                    if (image.Contains("text.gif") == false) continue;

                }

                {
                    // get the name

                    current_position = input.IndexOf("<td>", current_position);
                    current_position = input.IndexOf("<a href=", current_position);

                    int manifest_begin = input.IndexOf("\"", current_position) + 1;
                    int manifest_end = input.IndexOf("\"", manifest_begin + 1);

                    new_data.name = input.Substring(manifest_begin, manifest_end - manifest_begin);

                    new_data.url = "http://osw.didrole.com/src/" + new_data.name;

                    current_position = input.IndexOf("</td>", current_position);
                }

                {
                    // get the date
                    current_position = input.IndexOf("<td align=\"right\"", current_position);
                    int date_begin = input.IndexOf(">", current_position) + 1;
                    int date_end = input.IndexOf("  </td>", current_position);

                    string date_string = input.Substring(date_begin, date_end - date_begin);

                    new_data.date = DateTime.Parse(date_string);

                    current_position = input.IndexOf("</td>", current_position);
                }

                output.Add(new_data);
            }

            return output;
        }

        static void Main(string[] args)
        {
            WebClient client = new WebClient();

            string website = client.DownloadString("http://osw.didrole.com/src");

            Console.WriteLine(website);

            var data = ExtractData(website);

            foreach(var d in data)
            {
                var file_data = ExtractFileData(client.DownloadString(d.url));

                Console.WriteLine("{0} <{1}> {2}", d.manifest, d.url, d.date.ToShortDateString());

                Console.WriteLine("{");

                foreach (var f in file_data)
                {
                    Console.WriteLine("\t{0} <{1}> {2}", f.name, f.url, f.date.ToShortDateString());
                }

                Console.WriteLine("}");
                //Console.WriteLine(data);
            }

            Console.Read();
        }
    }
}
