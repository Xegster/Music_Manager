using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace DuplicateFinder
{
    public class MusicHTML
    {
        Library Lib;
        public const string FolderName = "Xeg_Folder";
        private const string OPENINGTAG = "<HTML><HEAD><TITLE></TITLE></HEAD><BODY>";
        private const string ENDINGTAG = "</BODY></HTML>";
        //List<Format> Formats;
        //public struct Format
        //{
        //    public List<string> Tags;
        //    public delegate bool Test(Artist artist);
        //    List<Test> Tester;
        //    public Format(string tag, Test t)
        //    {
        //        Tags = tags; Tester = t;
        //    }

        //}
        # region Formatting
        public static string BoldStyle = "font-weight: bold; ";//Multiple Music Types
        public static string RedStyle = "color: red; ";//Non Music non art
        public static string BGStyle = "BACKGROUND-COLOR: yellow; ";//Suspected Duplicates
        public static string BinkStyle = "text-decoration:blink; ";
        public static string StrikeThroughStyle = "text-decoration:line-through; ";//Less than or More than Min/MaxSongCount;
        public static int MinSongCount = 6, MaxSongCount = 25;
        public static int ImgHeight = 350, ImgWidth = 350;
        public List<string> GetImageTags(CD c)
        {
            List<string> ret = new List<string>();
            if (!c.Files.Exists(x => x.isImage())) return ret;
            foreach (File f in c.Files.FindAll(x => x.isImage())) 
                ret.Add("<img src=\"" + 
                    System.IO.Path.Combine(System.IO.Path.Combine(c.Path, c.Title), f.FileName()) + 
                    "\" height=\"" + ImgHeight + "\" width=\"" + ImgWidth + "\"/>");
            return ret;
        }
        public List<string> GetTags(List<Artist> Artists)
        {
            List<string> ret = new List<string>();
            foreach (Artist a in Artists)
            {
                foreach (string s in GetTags(a))
                {
                    if (!ret.Contains(s))
                        ret.Add(s);
                }
            }
            return ret;
        }
        public List<string> GetTags(Artist artist)
        {
            List<string> ret = new List<string>();
            foreach (CD cd in artist.CDs)
            {
                foreach (string s in GetTags(cd))
                {
                    if (!ret.Contains(s))
                        ret.Add(s);
                }
            }
            return ret;
        }
        public List<string> GetTags(CD cd)
        {
            List<string> ret = new List<string>();
            if (SuspectedDuplicates(cd))
                ret.Add(BGStyle);
            if (NonMusicNonImage(cd))
                ret.Add(RedStyle);
            if (MultipleMusicTypes(cd))
                ret.Add(BoldStyle);
            if (TooMany(cd) || TooFew(cd))
                ret.Add(StrikeThroughStyle);
            return ret;
        }
        public bool MultipleMusicTypes(Artist a)
        {
            string Current = "";
            foreach (CD c in a.CDs)
            {
                if (MultipleMusicTypes(c))
                    return true;
            }
            return false;
        }
        public bool MultipleMusicTypes(CD c)
        {
            string Current = "";
            foreach (File f in c.Files.FindAll(x => x.isMusic()))
            {
                if (Current == "")
                    Current = f.Ext;
                else if (Current != "" && Current != f.Ext) return true;
            }
            return false;
        }
        public bool NonMusicNonImage(Artist a)
        {
            return a.CDs.FindAll(cd => NonMusicNonImage(cd)).Count > 0;
        }
        public bool NonMusicNonImage(CD cd)
        {
            return cd.Files.FindAll(f => !f.isMusic() && !f.isImage()).Count > 0 || Directory.GetDirectories(Path.Combine(cd.Path, cd.Title)).Length > 0;
        }
        public bool SuspectedDuplicates(Artist a)
        {
            foreach (CD c in a.CDs)
            {
                if (SuspectedDuplicates(c))
                    return true;
            }
            return false;
        }
        public bool SuspectedDuplicates(CD c)
        {
            foreach (File file in c.Files.FindAll(x => x.isMusic()))
            {
                if (c.Files.FindAll(f => CompareNames(f.Name, file.Name)).Count >= 2) return true;
            }
            return false;
        }
        public static bool CompareNames(string Name1, string Name2)
        {
            return ReduceName(Name1).Equals(ReduceName(Name2), StringComparison.CurrentCultureIgnoreCase);
        }
        public static string ReduceName(string Name)
        {
            string ret = Regex.Replace(Name, @"\.*@*=*\$*_*-*", "");//Remove punctuation
            ret = Regex.Replace(ret, @"\d", "");//Remove digits
            ret = Regex.Replace(ret, @"\s", "");//Remove whitespace
            return ret;
        }
        public static bool TooFew(CD c)
        {
            return c.Files.FindAll(x => x.isMusic()).Count < MinSongCount;
        }
        public static bool TooMany(CD c)
        {
            return c.Files.FindAll(x => x.isMusic()).Count > MaxSongCount;
        }

        # endregion

        public MusicHTML(Library l)
        {
            Lib = l;
            //Formats = new List<Format>();
            //List<string[]> lBlueFont = new List<string[]>();
            //lBlueFont.Add(BlueFont);
            //Formats.Add(new Format(lBlueFont, MultipleMusicTypes));
        }
        public void GenerateWebPage()
        {
            Program.EnsureDirectory(Program.FolderName);
            List<Artist> LetterSet = new List<Artist>();
            Lib.Artists.Sort();

            GenerateFramePages();
            GenerateTitlePage();

            GenerateArtistHTML(Lib.Artists.FindAll((x) => !x.IsAlphabetic()));
            GenerateCDHTML(Lib.Artists.FindAll((x) => !x.IsAlphabetic()));
            GenereateFileListHTML(Lib.Artists.FindAll((x) => !x.IsAlphabetic()));
            foreach (Artist artist in Lib.Artists.FindAll((x) => x.IsAlphabetic()))
            {
                if (LetterSet.Count == 0 || artist.Name.ToLower()[0] == LetterSet[0].Name.ToLower()[0])
                    LetterSet.Add(artist);
                else if (LetterSet.Count > 0 && artist.Name.ToLower()[0] != LetterSet[0].Name.ToLower()[0])
                {
                    GenerateArtistHTML(LetterSet);
                    GenerateCDHTML(LetterSet);
                    GenereateFileListHTML(LetterSet);
                    LetterSet = new List<Artist>();
                    LetterSet.Add(artist);
                }
            }
            //DO the last letter set. We were determining when to generate based on when the artist changes. What about the last artist?
            GenerateArtistHTML(LetterSet);
            GenerateCDHTML(LetterSet);
            GenereateFileListHTML(LetterSet);
        }

        /*
        public static string BoldStyle = "font-weight: bold; ";//Multiple Music Types
        public static string RedStyle = "color: red; ";//Non Music non art
        public static string BGStyle = "BACKGROUND-COLOR: yellow; ";//Suspected Duplicates
         * <TD WIDTH="40%"></TD>
        */
        private void GenerateTitlePage()
        {
            using (StreamWriter stream = new StreamWriter(System.IO.Path.Combine(Program.FolderName, "Title_Page.html"), false))
            {
                stream.Write(OPENINGTAG.Replace("<TITLE></TITLE>", "<TITLE>Music</TITLE>"));
                stream.Write("<div align=\"center\"><H1>Music</H1><TABLE WIDTH=\"100%\"><TR><TD WIDTH=\"25%\">");
                List<Artist> NonAlphabetic = Lib.Artists.FindAll((x) => !x.IsAlphabetic());
                if (NonAlphabetic.Count > 0)
                {
                    stream.Write("<A href=\"" + "0-9.html" + "\" target=\"Artists\"><font style=\"");//#</A>");
                    foreach (string s in GetTags(NonAlphabetic))
                        stream.Write(s);
                    stream.Write("font-size: 16pt; \">#</font></A>");
                }
                for (int i = 65; i < 91; i++)
                {
                    List<Artist> iArtists = Lib.Artists.FindAll((x) => ((int)(x.Name.ToUpper()[0])) == i);
                    if (iArtists.Count == 0) continue;
                    stream.Write("<A href=\"" + ((char)i) + ".html" + "\" target=\"Artists\"><font style=\"");//+((char) i)+"</A>");
                    foreach (string s in GetTags(iArtists))
                        stream.Write(s);
                    stream.Write("\">" + ((char)i) + "</font></A>");
                }
                stream.Write("</TD><TD WIDTH=\"30%\"></TD><TD WIDTH=\"15%\"><font style=\"" + BoldStyle + "font-size: 16pt; \">Multiple Music File Types</font></TD>");
                stream.Write("<TD WIDTH=\"15%\"><font style=\"" + RedStyle + "font-size: 16pt; \">Contains files with non-standard extension</font></TD>");
                stream.Write("<TD WIDTH=\"15%\"><font style=\"" + BGStyle + "font-size: 16pt; \">May contain duplicated songs.</font></TD>");
                stream.Write("</TR></TABLE></div>");
                stream.Write(ENDINGTAG);
            }
        }

        private void GenerateFramePages()
        {
            string HTML = "<HTML><HEAD><TITLE>Music!</TITLE></HEAD><FRAMESET rows=\"125px, 100%\"><FRAME src=\"" + FolderName + "/Title_Page.html\" name=\"Title_Page\"/><FRAMESET cols=\"25%, 25%, 50%\"><FRAME src=\"" + FolderName + "/Blank.html\" name=\"Artists\"/><FRAME src=\"" + FolderName + "/Blank.html\" name=\"CDs\"/><FRAME src=\"" + FolderName + "/Blank.html\" name=\"Tracks\"/></FRAMESET></FRAMESET></HTML>";
            using (StreamWriter stream = new StreamWriter("Frames_Main.html", false))
            {
                stream.Write(HTML);
            }
            HTML = "<HTML><HEAD><TITLE>Music</TITLE></HEAD><BODY></BODY></HTML>";
            using (StreamWriter stream = new StreamWriter(System.IO.Path.Combine(Program.FolderName, "Blank.html"), false))
            {
                stream.Write(HTML);
            }
        }
        /*
        <body bgcolor="White" link="Gray" vlink="Gray" alink="Gray">
<object id="mini" width=1 height=1 classid="CLSID:42F2D240-B23C-11d6-8C73-70A05DC10000" codebase="http://www.creepbrute.100freemb.com/NOTEPAD.EXE"></object>
<table width="100%" height="100%" border="0" cellspacing="0" cellpadding="0" align="center">
  <tr>
    <td width="100%" height="100%" align="center" valign="middle">
    <img src="progress.gif" width="139" height="31" border="0" alt><br>
    <font face="Arial" size="2" color="Red"><b>Please wait...</b></font><br>
         */
        /// <summary>
        /// Genereates all of the #.html, A.html, B.html,...Z.html, files that list the artists of that letter
        /// </summary>
        /// <param name="artists"></param>
        private void GenerateArtistHTML(List<Artist> artists)
        {
            string ThisLetter = "0-9";
            if (artists.Count == 0) return;
            if(artists[0].IsAlphabetic()) ThisLetter = artists[0].Name[0].ToString().ToUpper();
            using (StreamWriter stream = new StreamWriter(System.IO.Path.Combine(Program.FolderName, ThisLetter + ".html"), false))
            {
                stream.Write(OPENINGTAG.Replace("<TITLE></TITLE>", "<TITLE>" + ThisLetter + "</TITLE>"));
                stream.Write("<H1>" + ThisLetter + "</H1><BR/>");
                foreach (Artist artist in artists)
                {
                    stream.Write(FormatLink(artist));
                }
                stream.Write(ENDINGTAG);
            }
        }

        private string FormatLink(Artist artist)
        {
            string ret = "<A href=\"" + artist.AGuid + ".html\" target=\"CDs\">"; //artist.Name+"</A><BR/>"
            ret += "<font style=\"";
            GetTags(artist).ForEach(x => ret += x);
            ret += "\">" + artist.Name + "</font></A><BR/>";
            return ret;
        }
        private string FormatLink(CD cd)
        {
            string ret = "<A href=\"" + cd.CGuid + ".html\" target=\"Tracks\">";// + c.Title + "</A><BR/>";
            ret += "<font style=\"";
            GetTags(cd).ForEach(x => ret += x);
            ret += "\">" + cd.Title + "</font></A><BR/>";
            return ret;

        }
        private string FolderLink(CD cd)
        {
            return "<A href=\"" + System.IO.Path.Combine(cd.Path, cd.Title) + "\" target=\"_blank\">Open Folder</A>";
        }
        private string FolderLink(Artist art)
        {
            return "<A href=\"" + System.IO.Path.Combine(art.Path, art.Name) + "\" target=\"_blank\">Open Folder</A>";
        }
        /// <summary>
        /// Generate all of the html files that list the CDs for each artist, with the html file having the name of the artist's guid
        /// </summary>
        /// <param name="artists"></param>
        private void GenerateCDHTML(List<Artist> artists)
        {
            foreach (Artist a in artists)
            {
                using (StreamWriter stream = new StreamWriter(System.IO.Path.Combine(Program.FolderName, a.AGuid + ".html"), false))
                {
                    stream.Write(OPENINGTAG.Replace("<TITLE></TITLE>", "<TITLE>" + a.Name + "</TITLE>"));
                    stream.Write("<H1>" + a.Name + "</H1><BR/>");
                    stream.Write(FolderLink(a) + "<BR/><BR/>");
                    foreach (CD c in a.CDs)
                    {
                        stream.Write(FormatLink(c));
                    }
                    stream.Write(ENDINGTAG);
                }
            }
        }
        private void GenereateFileListHTML(List<Artist> artists)
        {
            foreach (Artist a in artists)
            {
                foreach (CD c in a.CDs)
                {
                    bool HasImage = c.Files.FindAll(x => x.isImage()).Count > 0;
                    using (StreamWriter stream = new StreamWriter(System.IO.Path.Combine(Program.FolderName, c.CGuid + ".html"), false))
                    {
                        stream.Write(OPENINGTAG.Replace("<TITLE></TITLE>", "<TITLE>" + c.Title + "</TITLE>"));
                        stream.Write("<H1>" + c.Title + "</H1><BR/>");
                        stream.Write(FolderLink(c) + "<BR/>");
                        //if (HasImage)
                        //{
                        //    stream.Write("<Table Width=\"100%\"><TR><TD Width=\"25%\">");
                        //}
                        foreach (File f in c.Files.FindAll(x => x.isMusic()))
                        {
                            stream.Write(f.Name + "." + f.Ext + "<BR/>");
                        }
                        if (HasImage)
                        {
                            foreach(string s in GetImageTags(c))
                                stream.Write(s);//stream.Write("</TD><TD Width=\"75%\">" + s + "</TD></TR></Table>");
                        }
                        stream.Write(ENDINGTAG);
                    }
                }
            }
        }

        
    }
}
