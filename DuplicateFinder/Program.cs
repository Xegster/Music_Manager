using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using sFile = System.IO.File;
using System.Xml;
using System.Xml.XPath;

namespace DuplicateFinder
{
    
    #region MainProgram

    class Program
    {
        public const string FolderName = "Xeg_Folder";
        public const string LibraryName = "library.xeg";
        enum SubFunction
        {
            NA,
            LIBRARY,
            ANALYZE,
            DELETE,
            CURRENT,
            HTML,
            STATS,
            FILENAMEEDIT,
            FILENAMEREPLACE,
            ITUNESXML
        }
        private static SubFunction SubFunctionFromAnswer(string answer)
        {
            if (answer.Contains("1") || answer.ToLower().Contains("library"))
                return SubFunction.LIBRARY;
            else if (answer.Contains("2") || answer.ToLower().Contains("analyze"))
                return SubFunction.ANALYZE;
            else if (answer.Contains("3") || answer.ToLower().Contains("delete"))
                return SubFunction.DELETE;
            else if (answer.Contains("4") || answer.ToLower().Contains("current"))
                return SubFunction.CURRENT;
            else if (answer.Contains("5") || answer.ToLower().Contains("html"))
                return SubFunction.HTML;
            else if (answer.Contains("6") || answer.ToLower().Contains("stats"))
                return SubFunction.STATS;
            else if (answer.Contains("7"))
                return SubFunction.FILENAMEEDIT;
            else if (answer.Contains("8"))
                return SubFunction.FILENAMEREPLACE;
            else if (answer.Contains("9"))
                return SubFunction.ITUNESXML;
            else return SubFunction.NA;
        }
        static void Main(string[] args)
        {
            string answer; Library l;
            SubFunction ans = SubFunction.NA;
            do
            {
                Console.WriteLine("1.) Library\n2.) Analyze\n3.) Delete\n4.) Current\n5.) HTML\n6.) Stats\n7.) File Name Edit\n8.) File Name Replace\n9.) iTunes XML");
                //string answer = Console.ReadLine();
                ans = SubFunctionFromAnswer(Console.ReadLine());

                switch (ans)
                {
                    case SubFunction.LIBRARY:
                        Console.WriteLine("Append to existing? y/n");
                        string append = Console.ReadLine();
                        bool bAppend = append.ToLower().Contains("y");

                        Console.WriteLine("1.) Current Directory.\n2.) Single Directory\n3.) Multiple Directories");
                        string dirtype = Console.ReadLine();

                        if (dirtype == "1")
                        {
                            l = new Library(Directory.GetCurrentDirectory(), bAppend);
                        }
                        else if (dirtype == "2")
                        {
                            Console.WriteLine("Specify directory:");
                            l = new Library(Console.ReadLine(), bAppend);
                        }
                        else if (dirtype == "3")
                        {
                            Console.WriteLine("Specify directories, quit to stop:");
                            string muldir = Console.ReadLine();
                            while (muldir != "quit")
                            {
                                l = new Library(muldir, bAppend);
                                Console.WriteLine("Next:\n");
                                muldir = Console.ReadLine();
                                if (!bAppend) bAppend = true;
                            }
                        }
                        break;
                    case SubFunction.ANALYZE:
                        Console.WriteLine("Specify the directory? Either \"no\" or a complete file path.");
                        answer = Console.ReadLine();
                        if (answer.ToLower() == "no")
                            AnalyzeDirectory(Directory.GetCurrentDirectory());
                        else
                            AnalyzeDirectory(answer);
                        break;
                    case SubFunction.DELETE:
                        Console.WriteLine("Please specify a file path: ");
                        string FileName = Console.ReadLine();
                        DeleteSpecified(FileName);
                        break;
                    case SubFunction.CURRENT:
                        Console.Write(Directory.GetCurrentDirectory());
                        answer = Console.ReadLine();
                        break;
                    case SubFunction.HTML:
                        MusicHTML music = new MusicHTML(Library.ImportExistingLibrary());
                        music.GenerateWebPage();
                        break;
                    case SubFunction.STATS:
                        l = Library.ImportExistingLibrary();
                        Console.WriteLine("Artists: " + l.Artists.Count);
                        int CDs = 0, files = 0, EmptyCD = 0, EmptyArtist = 0;
                        foreach (Artist a in l.Artists)
                        {
                            if (a.CDs.Count == 0) EmptyArtist++;
                            foreach (CD cd in a.CDs)
                            {
                                CDs++;
                                if (cd.Files.Count == 0) EmptyCD++;
                                foreach (File f in cd.Files)
                                    files++;
                            }
                        }
                        Console.WriteLine("CDs: " + CDs);
                        Console.WriteLine("Files: " + files);
                        Console.WriteLine("Empty CD: " + EmptyCD);
                        Console.WriteLine("Empty Artist: " + EmptyArtist);
                        string s = Console.ReadLine();

                        break;
                    case SubFunction.FILENAMEEDIT:
                        Console.WriteLine("Remove _ except for in the cases: I_m, won_t, can_t, droppin_, let_s, it_s. Remove -.\n\n" +
                            "Please specify a file path (quit to exit): ");
                        answer = Console.ReadLine();
                        while (answer.ToLower() != "quit")
                        {
                            FixFileNames(answer);
                            Console.WriteLine("Please specify a file path (quit to exit): ");
                            answer = Console.ReadLine();
                        }
                        break;
                    case SubFunction.FILENAMEREPLACE:
                        Console.WriteLine("Please specify a file path (quit to exit): ");
                        answer = Console.ReadLine();
                        while (answer.ToLower() != "quit")
                        {
                            Console.WriteLine("Specify Word to be replaced, word to replace with, enter in between: ");
                            string ToBeReplaced = Console.ReadLine();
                            string ToReplace = Console.ReadLine();
                            ReplaceFileNames(answer, ToBeReplaced, ToReplace);
                            Console.WriteLine("Please specify a file path (quit to exit): ");
                            answer = Console.ReadLine();
                        }
                        break;
                    case SubFunction.ITUNESXML:
                        Console.WriteLine("1.) Import\n2.) Export\n3.) Quit");
                        answer = Console.ReadLine();
                        switch (answer)
                        {
                            case "3": break;
                            case "2":

                                break;
                            case "1":
                                Console.WriteLine("Complete filename:");
                                answer = Console.ReadLine();
                                XmlDocument doc = new XmlDocument();
                                doc.Load(answer);
                                l = new Library(doc);
                                break;
                            default: break;
                        }
                        break;
                    case SubFunction.NA:

                        break;
                }
            } while (ans != SubFunction.NA);
        }


        public static void EnsureDirectory(string Path)
        {
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);
        }

        #region BasicFunctions

        static void ReplaceFileNames(string Path, string ToBeReplaced, string ToReplace)
        {
            string[][] FileNames = GetFileList(Path);
            string[][] Replaced = GetFileList(Path);
            
            for (int i = 0; i < FileNames.GetLength(0); i++)
            {
                if (!FileNames[i][1].Contains(ToBeReplaced)) continue;
                Replaced[i][1] = FileNames[i][1].Replace(ToBeReplaced, ToReplace);
                System.IO.File.Move(CombineFilePath(FileNames[i]), CombineFilePath(Replaced[i]));
            }

        }

        static string[][] GetFileList(string Path)
        {
            string[] _tempfiles = Directory.GetFiles(Path, "*", SearchOption.AllDirectories);

            string[][] ret = new string[_tempfiles.Length][];

            for (int i = 0; i < _tempfiles.Length; i++)
            {
                ret[i] = SplitFilePath(_tempfiles[i]);
            }

            return ret;
        }

        static string[] SplitFilePath(string FilePath)
        {
            return new string[] { System.IO.Path.GetDirectoryName(FilePath), System.IO.Path.GetFileNameWithoutExtension(FilePath),
            System.IO.Path.GetExtension(FilePath)};
        }

        static string CombineFilePath(string[] FilePathParts)
        {
            if (FilePathParts.Length == 1) return FilePathParts[0];
            else if (FilePathParts.Length == 2) return System.IO.Path.Combine(FilePathParts[0], FilePathParts[1]);
            else return System.IO.Path.Combine(FilePathParts[0], FilePathParts[1] + FilePathParts[2]);
        }

        static void FixFileNames(string DirectoryPath)
        {
            //string[] _tempfiles = Directory.GetFiles(DirectoryPath, "*", SearchOption.AllDirectories);

            string[][] OriginalFileNames = GetFileList(DirectoryPath);
            string[][] FixedFileNames = GetFileList(DirectoryPath);

            //for (int i = 0; i < _tempfiles.Length; i++)
            //{
            //    OriginalFileNames[i] = new string[] {System.IO.Path.GetDirectoryName(_tempfiles[i]), System.IO.Path.GetFileName(_tempfiles[i])};
            //    FixedFileNames[i] = new string[] { System.IO.Path.GetDirectoryName(_tempfiles[i]), System.IO.Path.GetFileName(_tempfiles[i]) };
            //}

            //First build the corrected file names
            for (int i = 0; i < FixedFileNames.GetLength(0); i++)
            {
                FixedFileNames[i][1] = FixedFileNames[i][1].Replace("I_m ", "I'm ");
                FixedFileNames[i][1] = FixedFileNames[i][1].Replace("_s ", "'s ");
                FixedFileNames[i][1] = FixedFileNames[i][1].Replace("n_t ", "n't ");
                FixedFileNames[i][1] = FixedFileNames[i][1].Replace("n_ ", "n' ");
                FixedFileNames[i][1] = FixedFileNames[i][1].Replace("_", " ");
                FixedFileNames[i][1] = FixedFileNames[i][1].Replace("-", " ");
                FixedFileNames[i][1] = Regex.Replace(FixedFileNames[i][1], " {2,}", " ");
                FixedFileNames[i][1] = FixedFileNames[i][1].Trim();
            }
            ////then rename the files
            for (int i = 0; i < FixedFileNames.GetLength(0); i++)
            {
                if (FixedFileNames[i][1] == OriginalFileNames[i][1]) continue;
                System.IO.File.Move(CombineFilePath(OriginalFileNames[i]), CombineFilePath(FixedFileNames[i]));
            }
        }

        static void DeleteSpecified(string FileName)
        {
            string FullPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), FileName);
            string ErrorPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Errors.txt");
            List<string[]> Errors = new List<string[]>();
            using (StreamReader sr = new StreamReader(FullPath))
            {
                while (!sr.EndOfStream)
                {
                    string path = sr.ReadLine();
                    if (sFile.Exists(path))
                    {
                        try
                        {
                            sFile.Delete(path);
                        }
                        catch(Exception e)
                        {
                            Errors.Add(new string[] { path, e.Message });
                        }
                    }
                }
            }
            if (Errors.Count > 0)
            {
                using (StreamWriter sr = new StreamWriter(ErrorPath, true))
                {
                    foreach (string[] error in Errors)
                        sr.WriteLine(error[0] + "\t\t\t" + error[1]);
                }
            }
        }

        #endregion

        #region Analysis
        static void AnalyzeDirectory(string Dir)
        {
            string CurrentDirectory = Dir;
            //We assume we're in the directory containing hte artists, so grab all those directories
            string[] Artists = null;
            try
            {
                Artists = System.IO.Directory.GetDirectories(CurrentDirectory);
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            //Analyze any artists and if they have duplicates, add them to our list
            List<string> DupArts = new List<string>();
            foreach (string artist in Artists)
            {
                if (AnalyzeArtist(artist))
                    DupArts.Add(artist.Substring(artist.LastIndexOf('\\') + 1));
            }
            //If we found any duplictes, output a text file containing the artist names
            if (DupArts.Count > 0)
            {
                using (StreamWriter sr = new StreamWriter(System.IO.Path.Combine(CurrentDirectory, "Artists.txt"), false))
                {
                    foreach (string DupArt in DupArts)
                        sr.WriteLine(DupArt);
                }
            }
        }

        static bool AnalyzeArtist(string ArtistPath)
        {
            //The artist folder should have a bunch of CD folders in it. Grab them
            string[] CDs = null;
            try
            {
                CDs = System.IO.Directory.GetDirectories(ArtistPath);
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            List<string[]> Duplicates = new List<string[]>();

            //Analyze each cd and collect the duplicates
            foreach(string CD in CDs)
                Duplicates.AddRange(AnalyzeCD(CD));

            //If we didn't find any dups in any cd, return false
            if (Duplicates.Count == 0) return false;

            string fileLoc = ArtistPath;
            string Artist = ArtistPath.Substring(ArtistPath.LastIndexOf('\\')+1);
            int MaxCols = -1;
            //It's possible that there are 3+ way ties between file names. WE'll make that many text files. But first, we need to 
            //thisisaname   thisisanothername   thisisonemorename
            //each column goes in a new file. Then we can use beyond compare to make quick comparisons between the file names
            Duplicates.ForEach(x =>{ if(x.Length > MaxCols) MaxCols = x.Length;});
            if(MaxCols <= 0) return false;

            List<FileInfo> OutPut = new List<FileInfo>();
            
            for(int i = 0; i < MaxCols; i++)
                OutPut.Add(new FileInfo(System.IO.Path.Combine(fileLoc, Artist + i.ToString() + ".txt")));
            try
            {
                int CurCol = 0;
                foreach (FileInfo fi in OutPut)
                {
                    using (StreamWriter sr = new StreamWriter(fi.FullName, false))
                    {
                        foreach (string[] Duplicate in Duplicates.FindAll(x => x.Length > CurCol))
                            if (CurCol < Duplicate.Length)
                                sr.WriteLine(Duplicate[CurCol]);
                    }
                    CurCol++;
                }
            }
            catch { }
            return true;

        }

        static List<string[]> AnalyzeCD(string CDPath)
        {
            string[] SongPaths = null;
            List<string[]> dups = new List<string[]>();
            try
            {
                SongPaths = System.IO.Directory.GetFiles(CDPath);
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }

            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }
            List<FileInfo> SongInfos = new List<FileInfo>();
            foreach (string SongPath in SongPaths)
            {
                try
                {
                    SongInfos.Add(new System.IO.FileInfo(SongPath));
                }
                catch (System.IO.FileNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
            }
            //ITerate over each fileinfo
            foreach (FileInfo fi in SongInfos.ToArray())
            {
                //Fine all the FileInfos with matching size (the length attribute) and names that are almost the same
                List<FileInfo> fiDups = SongInfos.FindAll(x => MusicHTML.CompareNames(x.Name, fi.Name) && x.Length == fi.Length);
                //if we can find more than one FileInfo like this, add them to our list for this CD
                if (fiDups.Count > 1)
                {
                    List<string> names = fiDups.ConvertAll<string>(new Converter<FileInfo, string>(x => x.FullName));
                    string[] aNames = names.ToArray();
                    if(!dups.Exists(x => x[0] == aNames[0] && x[1] == aNames[1]))
                        dups.Add(aNames);
                }
            }
            return dups;
        }
        #endregion
    }
    #endregion

}
