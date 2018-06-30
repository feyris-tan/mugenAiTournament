using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;

namespace mugenAiTournament
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Run with your Mugen installation path specified, please.");
                return;
            }

            new Program().Run(new DirectoryInfo(args[0]));
        }


        public void Run(DirectoryInfo di)
        {
            mugenWorkingDirectory = di;
            mugenExe = new FileInfo(Path.Combine(di.FullName, "mugen.exe"));
            if (!mugenExe.Exists)
            {
                Console.WriteLine("{0} was not found.",mugenExe);
                return;
            }

            DirectoryInfo charsPath = new DirectoryInfo(Path.Combine(di.FullName, "chars"));
            if (!charsPath.Exists)
            {
                Console.WriteLine("{0} was not found.",charsPath);
                return;
            }

            DirectoryInfo stagesPath = new DirectoryInfo(Path.Combine(di.FullName, "stages"));
            if (!stagesPath.Exists)
            {
                Console.WriteLine("{0} was not found.", stagesPath);
                return;
            }

            //Felder initialisieren
            FileInfo[] stageFileInfos = stagesPath.GetFiles("*.def", SearchOption.TopDirectoryOnly);
            string[] stageNames = Array.ConvertAll(stageFileInfos, x => Path.GetFileNameWithoutExtension(x.Name));
            DirectoryInfo[] charsDirectoryInfo = charsPath.GetDirectories("*", SearchOption.TopDirectoryOnly);
            string[] charNames = Array.ConvertAll(charsDirectoryInfo, x => x.Name);
            int numChars = charNames.Length;
            Random rng = new Random();
            Nullable<double>[][] scoreboard = new Nullable<double>[numChars][];
            List<FightInfo> fights = new List<FightInfo>();
            
            //Pairings bestimmen
            for (int y = 0; y < numChars; y++)
            {
                scoreboard[y] = new Nullable<double>[numChars];
                for (int x = 0; x < numChars; x++)
                {
                    if (x == y)
                    {
                        scoreboard[x][y] = null;
                        continue;
                    }

                    FightInfo fi = new FightInfo();
                    fi.Player1 = charNames[x];
                    fi.Player2 = charNames[y];
                    fi.Player1Id = x;
                    fi.Player2Id = y;
                    fi.Stage = stageNames[rng.Next(stageNames.Length)];

                    if (!fights.Contains(fi))
                    {
                        fights.Add(fi);
                    }
                }
            }
            
            //Reihenfolge durchwürfeln
            for (int i = 0; i < fights.Count * 2; i++)
            {
                int idxA = rng.Next(fights.Count);
                int idxB = rng.Next(fights.Count);

                FightInfo helperVar = fights[idxA];
                fights[idxA] = fights[idxB];
                fights[idxB] = helperVar;
            }
            
            //Kämpfe austragen
            while (fights.Count > 0)
            {
                FightInfo current = fights[0];
                fights.RemoveAt(0);
                WinningTeam wt = Fight(current.Player1, current.Player2, current.Stage);
                switch (wt)
                {
                    case WinningTeam.Player1:
                        scoreboard[current.Player1Id][current.Player2Id] = 1.0f;
                        scoreboard[current.Player2Id][current.Player1Id] = 0.0f;
                        break;
                    case WinningTeam.Player2:
                        scoreboard[current.Player1Id][current.Player2Id] = 0.0f;
                        scoreboard[current.Player2Id][current.Player1Id] = 1.0f;
                        break;
                    case WinningTeam.Draw:
                        scoreboard[current.Player1Id][current.Player2Id] = 0.5f;
                        scoreboard[current.Player2Id][current.Player1Id] = 0.5f;
                        break;
                    default:
                        throw new NotImplementedException(wt.ToString());
                }
            }

            //Scoreboard speichern
            FileStream outStream = File.OpenWrite("output.csv");
            StreamWriter streamWriter = new StreamWriter(outStream, Encoding.ASCII);
            streamWriter.Write(";");
            for (int i = 0; i < charNames.Length; i++)
            {
                streamWriter.Write(charNames[i]);
                streamWriter.Write(";");
            }
            streamWriter.WriteLine();
            for (int y = 0; y < charNames.Length; y++)
            {
                streamWriter.Write(charNames[y]);
                streamWriter.Write(";");
                double score = 0.0f;
                for (int x = 0; x < charNames.Length; x++)
                {
                    if (scoreboard[y][x].HasValue)
                    {
                        streamWriter.Write(scoreboard[y][x].Value);
                        score += scoreboard[y][x].Value;
                    }
                    streamWriter.Write(";");
                }
                streamWriter.Write(score);
                streamWriter.Write(";");
                streamWriter.WriteLine(";");
            }
            streamWriter.Flush();
            streamWriter.Close();
            streamWriter.Dispose();
        }

        private FileInfo mugenExe;
        private DirectoryInfo mugenWorkingDirectory;

        private WinningTeam Fight(string player1, string player2, string stage)
        {
            ProcessStartInfo psi = new ProcessStartInfo(mugenExe.FullName);
            psi.WorkingDirectory = mugenWorkingDirectory.FullName;
            psi.UseShellExecute = false;
            psi.Arguments = String.Format("-log log.txt -p1 \"{0}\" -p1.ai 1 -p2 \"{1}\" -p2.ai 1 -rounds 1 -s \"{2}\" -nojoy", player1, player2, stage);
            psi.WindowStyle = ProcessWindowStyle.Maximized;
            
            Process p = new Process();
            p.StartInfo = psi;
            p.Start();
            Thread.Sleep(1000);
            p.WaitForExit();

            FileInfo fi = new FileInfo(Path.Combine(mugenWorkingDirectory.FullName, "log.txt"));
            string[] lines = File.ReadAllLines(fi.FullName);
            foreach (string line in lines)
            {
                if (line.StartsWith("winningteam"))
                {
                    string[] lineArgs = line.Split(' ');
                    lineArgs[1] = lineArgs[1].Trim();
                    fi.Delete();
                    switch (lineArgs[2].ToCharArray()[0])
                    {
                        case '1':
                            return WinningTeam.Player1;
                        case '2':
                            return WinningTeam.Player2;
                        case '0':
                            return WinningTeam.Draw;
                        default:
                            throw new Exception("Could not determine winning team: " + line);
                    }
                }
            }

            throw new Exception("Could not determine winning team.");
        }
    }
}