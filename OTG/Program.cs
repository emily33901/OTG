using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Handlers;
using System.IO;
using System.Net;
using LibGit2Sharp;
using System.Timers;

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
        static Timer t;

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

        static List<FileData> ExtractFileData(string input, string manifest)
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

                    if (image.Contains("text.gif") == false && image.Contains("binary.gif") == false && image.Contains("unknown.gif") == false) continue;

                }

                {
                    // get the name

                    current_position = input.IndexOf("<td>", current_position);
                    current_position = input.IndexOf("<a href=", current_position);

                    int manifest_begin = input.IndexOf("\"", current_position) + 1;
                    int manifest_end = input.IndexOf("\"", manifest_begin + 1);

                    new_data.name = input.Substring(manifest_begin, manifest_end - manifest_begin);

                    new_data.url = "http://osw.didrole.com/src/" + manifest + "/" + new_data.name;

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

        private static void RunUpdate()
        {
            Console.WriteLine("Beginning Update...");

            // Get the latest update from the server
            WebClient client = new WebClient();

            string website = client.DownloadString("http://osw.didrole.com/src");

            Console.WriteLine(website);

            var data = ExtractData(website);

            UpdateData latest_update = new UpdateData();

            latest_update.date = new DateTime(0);

            foreach (var d in data)
            {
                if (d.date > latest_update.date) latest_update = d;
            }

            var file_data = ExtractFileData(client.DownloadString(latest_update.url), latest_update.manifest);

            string header_file = string.Format("{0} <{1}> {2}\n", latest_update.manifest, latest_update.url, latest_update.date.ToShortDateString());

            Console.WriteLine(header_file);

            // now push to git

            // get our username + password from the secrets.json file
            dynamic secrets = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<dynamic>(File.ReadAllText("secrets.json"));

            string username = secrets["username"];
            string password = Encoding.UTF8.GetString(Convert.FromBase64String(secrets["password"]));

            // check the manifest that is already there
            try
            {
                string old_manifest = File.ReadAllText("AutoOSW/manifest.txt");

                // this is already up to date we dont need to do anything
                if (old_manifest == latest_update.manifest + "\n")
                {
                    Console.WriteLine("Up to date... No action taken.");
                    return;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("No existing repo detected...");
                // repo wasnt initialised properly before... this is fine
            }

            Console.WriteLine("Prepare for clone...");

            // clone the repo to make sure we have it
            const string repo_url = "https://github.com/josh33901/AutoOSW.git";

            Console.WriteLine("Removing old repo...");
            try
            {
                Directory.Move("AutoOSW/", "AutoOSWOld/");
                var d = new DirectoryInfo("AutoOSWOld/");
                foreach (var file in d.GetFiles("*", SearchOption.AllDirectories))
                    file.Attributes &= ~FileAttributes.ReadOnly;

                Directory.Delete("AutoOSWOld/", true);

            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e.Message);
            }
            Console.WriteLine("Done.");

            Console.WriteLine("Cloning original repo...");

            string git_path = Repository.Clone(repo_url, "AutoOSW/");

            Console.WriteLine("Done.");

            Console.WriteLine("Downloading files from source...");
            // download all the files into this directory
            foreach (var f in file_data)
            {
                Console.WriteLine("({0} <{1}>)", f.name, f.url);
                client.DownloadFile(f.url, "AutoOSW/" + f.name);
            }
            Console.WriteLine("Done.");

            Console.WriteLine("Writing manifest file...");
            File.WriteAllText("AutoOSW/manifest.txt", latest_update.manifest + "\n");

            // now that we have all the files git add them

            using (var repo = new Repository("AutoOSW/"))
            {
                Console.WriteLine("Staging repo...");
                Commands.Stage(repo, "*");
                Console.WriteLine("Done.");

                // Create the committer's signature and commit
                Console.WriteLine("Commiting to repo...");
                Signature author = new Signature("josh33901", "josh33901@gmail.com", DateTime.Now);
                Signature committer = author;

                string commit_message = "Update for manifest " + latest_update.manifest + " @ " + latest_update.date.ToShortDateString();

                // Commit to the repository
                try
                {
                    Commit commit = repo.Commit(commit_message, author, committer);

                }
                catch (EmptyCommitException)
                {
                    Console.WriteLine("No changes detected... Not committing.");
                    return;
                }
                Console.WriteLine("Done.");

                Console.WriteLine("Pushing to origin/master");

                Remote remote = repo.Network.Remotes["origin"];
                var options = new PushOptions
                {
                    CredentialsProvider = (_url, _user, _cred) =>
                        new UsernamePasswordCredentials { Username = username, Password = password }
                };
                repo.Network.Push(remote, @"refs/heads/master", options);
                Console.WriteLine("Done.");

                Console.WriteLine("Action Finished.");
            }
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            RunUpdate();
        }

        static void Main(string[] args)
        {
            if(args.Contains("-timedevent"))
            {
                // tick every 24 hours
                t = new Timer(1000 * 60 * 60 * 24);

                t.Elapsed += OnTimedEvent;
                t.Enabled = true;

                GC.KeepAlive(t);

                Console.ReadLine();
            }
            else
            {
                RunUpdate();
            }
        }
    }
}
