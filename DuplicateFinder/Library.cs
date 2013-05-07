using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.IO;
using sFile = System.IO.File;
using System.Xml;
using System.Xml.XPath;


namespace DuplicateFinder
{
    [Serializable()]
    public class Library : ISerializable
    {
        public static List<string> MusicExtensions = new List<string>(new string[] {
            "mp3","wma","m4a","wav","flac","ape","aac","atrac","tta","ogg","m4p","mp4"
        });
        public static List<string> ArtworkExtensions = new List<string>(new string[] {
        "jpg","jpeg","png","gif","tif","tiff", "bmp", "pdf"
        });


        public Library(XmlDocument iTunesLibraryXML)
        {
            List<File> MyTrackList = new List<File>();
            XmlNode TrackParent = iTunesLibraryXML.SelectSingleNode("/plist/dict").RemoveChild(iTunesLibraryXML.SelectSingleNode("/plist/dict/dict"));
            XmlNode PlayListParent = iTunesLibraryXML.SelectSingleNode("/plist/dict").RemoveChild(iTunesLibraryXML.SelectSingleNode("/plist/dict/array"));

            List<Dictionary<string, string>> DictTrackList = new List<Dictionary<string, string>>();
            Dictionary<string, string> dTrack;
            foreach (XmlNode Track in TrackParent.SelectNodes("dict"))
            {
                dTrack = new Dictionary<string, string>();

                foreach (XmlNode TrackAtt in Track.SelectNodes("node()[contains(name(), 'key')]"))
                {
                    dTrack.Add(TrackAtt.InnerText, TrackAtt.NextSibling.InnerText);
                }

                DictTrackList.Add(dTrack);
                MyTrackList.Add(new File(dTrack));
            }

            foreach (File Track in MyTrackList)
            {

            }

        }

        public List<Artist> Artists;
        public Library(List<Artist> artists)
        {
            Artists = artists;
        }
        public Library(SerializationInfo info, StreamingContext context)
        {
            Artists = (List<Artist>)info.GetValue("Artists", typeof(List<Artist>));
        }
        public Library(string dir, bool append)
        {
            Library Existing = null;
            if (append && LibraryFileExists()) Existing = ImportExistingLibrary();
            string[] dArtists = null;
            string[] dCDs = null;
            string[] dTracks = null;
            List<File> Files = new List<File>();
            List<CD> CDs = new List<CD>();
            List<Artist> Artists = new List<Artist>();
            try
            {
                dArtists = System.IO.Directory.GetDirectories(dir);
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            foreach (string artist in dArtists)
            {
                try
                {
                    dCDs = System.IO.Directory.GetDirectories(artist);
                }
                catch { }
                foreach (string dCD in dCDs)
                {
                    try
                    {
                        dTracks = System.IO.Directory.GetFiles(dCD);
                    }
                    catch { }
                    foreach (string dTrack in dTracks)
                    {
                        Files.Add(new File(dTrack));
                    }
                    Files.Sort();
                    CD SampleCD = new CD(dCD, Files);
                    CDs.Add(new CD(dCD, Files));
                    Files = new List<File>();
                }
                CDs.Sort();
                Artists.Add(new Artist(artist, CDs));
                CDs = new List<CD>();
            }
            Artists.Sort();
            Library lib = new Library(Artists);

            if (Existing != null && Existing.Artists.Count > 0)
                lib.AddArtists(Existing.Artists);
            string s = "";
            try
            {
                Program.EnsureDirectory(Program.FolderName);
                using (Stream stream = sFile.Open(System.IO.Path.Combine(Program.FolderName, Program.LibraryName), FileMode.Create))
                {
                    BinaryFormatter bFormatter = new BinaryFormatter();
                    bFormatter.Serialize(stream, lib);
                }
            }
            catch (Exception e)
            {

            }
        }
        public static Library ImportExistingLibrary()
        {
            Library l;
            using (Stream stream = sFile.Open(System.IO.Path.Combine(Program.FolderName, Program.LibraryName), FileMode.Open))
            {
                BinaryFormatter bformatter = new BinaryFormatter();
                l = (Library)bformatter.Deserialize(stream);
            }
            return l;
        }
        public static bool LibraryFileExists()
        {
            return System.IO.File.Exists(System.IO.Path.Combine(Program.FolderName, Program.LibraryName));
        }
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Artists", Artists);
        }
        public void AddArtists(List<Artist> NewArtists)
        {
            foreach (Artist artist in NewArtists)
            {
                if (Artists.Contains(artist))
                {
                    Artist ExArt = Artists.Find(x => x.Equals(artist));
                    foreach (CD cd in artist.CDs)
                    {
                        if (ExArt.CDs.Contains(cd))
                        {
                            CD ExCD = ExArt.CDs.Find(x => x.Equals(cd));
                            foreach (File f in cd.Files)
                            {
                                if (!ExCD.Files.Contains(f))
                                    ExCD.Files.Add(f);
                            }
                        }
                        else
                            ExArt.CDs.Add(cd);
                    }
                }
                else
                    Artists.Add(artist);
            }
        }
    }
    [Serializable()]
    public class Artist : ISerializable, IComparable
    {
        public string Name, Path;
        public List<CD> CDs;
        public Guid AGuid;
        public Artist(string path, List<CD> cds)
        {
            if (path.Contains("/"))
            {
                Name = path.Substring(path.LastIndexOf('/') + 1);
                Path = path.Remove(path.LastIndexOf('/'));
            }
            else if (path.Contains("\\"))
            {
                Name = path.Substring(path.LastIndexOf('\\') + 1);
                Path = path.Remove(path.LastIndexOf('\\'));
            }
            CDs = cds;
            AGuid = Guid.NewGuid();
        }
        public Artist(string name, string path, List<CD> cds)
        {
            Name = name;
            Path = path;
            CDs = cds;
            AGuid = Guid.NewGuid();
        }
        protected Artist(SerializationInfo info, StreamingContext context)
        {
            Name = (string)info.GetString("Name");
            Path = (string)info.GetString("Path");
            CDs = (List<CD>)info.GetValue("CDs", typeof(List<CD>));
            AGuid = (Guid)info.GetValue("AGuid", typeof(Guid));
        }
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", Name);
            info.AddValue("Path", Path);
            info.AddValue("CDs", CDs);
            info.AddValue("AGuid", AGuid);
        }
        public int CompareTo(object art)
        {
            return typeof(Artist) == art.GetType() ? Name.CompareTo(((Artist)art).Name) : -1;
        }
        public bool IsAlphabetic()
        {
            return Regex.Match(Name.Substring(0, 1), "[a-zA-Z]").Success;
        }
        public override bool Equals(System.Object obj)
        {
            if (obj.GetType() != typeof(Artist)) return false;
            return Equals(obj as Artist);
        }
        public bool Equals(Artist artist)
        {
            return Name.Equals(artist.Name, StringComparison.CurrentCultureIgnoreCase);
        }
    }
    [Serializable()]
    public class CD : ISerializable, IComparable
    {
        public string Title, Path;
        public List<File> Files;
        public Guid CGuid;
        public CD(string filepath, List<File> files)
        {
            if (filepath.Contains("/"))
            {
                Title = filepath.Substring(filepath.LastIndexOf('/') + 1);
                Path = filepath.Remove(filepath.LastIndexOf('/'));
            }
            else if (filepath.Contains("\\"))
            {
                Title = filepath.Substring(filepath.LastIndexOf('\\') + 1);
                Path = filepath.Remove(filepath.LastIndexOf('\\'));
            }
            Files = files;
            CGuid = Guid.NewGuid();
        }
        public CD(string title, string path, File cover, List<File> tracks)
        {
            Title = title;
            Path = path;
            Files = tracks;
            CGuid = Guid.NewGuid();
        }
        protected CD(SerializationInfo info, StreamingContext context)
        {
            Title = (string)info.GetString("Title");
            Path = (string)info.GetString("Path");
            Files = (List<File>)info.GetValue("Tracks", typeof(List<File>));
            CGuid = (Guid)info.GetValue("CGuid", typeof(Guid));
        }
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Title", Title);
            info.AddValue("Path", Path);
            info.AddValue("Tracks", Files);
            info.AddValue("CGuid", CGuid);
        }
        public int CompareTo(object cd)
        {
            return typeof(CD) == cd.GetType() ? Title.CompareTo(((CD)cd).Title) : -1;
        }
        public override bool Equals(System.Object obj)
        {
            if (obj.GetType() != typeof(CD)) return false;
            return Equals(obj as CD);
        }
        public bool Equals(CD cd)
        {
            return Title.Equals(cd.Title, StringComparison.CurrentCultureIgnoreCase) && 
                System.IO.Path.Equals(Path, cd.Path);
        }
    }
    [Serializable()]
    public class File : ISerializable, IComparable
    {
        //basic variables
        public string Name;
        public string Ext;
        bool Music, Artwork, Set = false;
        public Guid FGuid;
        DateTime DateAdded;

        //advanced variables. Shared with iTunes
        string Genre, ExtType;
        int Size, Time, TrackNumber, BitRate;
        
        //iTunes specific variables
        string iTunesID;

        public File(Dictionary<string, string> iTunesTrackDict)
        {
            iTunesTrackDict.TryGetValue("Name", out Name);
            iTunesTrackDict.TryGetValue("Genre", out Genre);
            iTunesTrackDict.TryGetValue("Kind", out ExtType);
            iTunesTrackDict.TryGetValue("Persistent ID", out iTunesID);
            FGuid = Guid.NewGuid();

            string sSize;
            iTunesTrackDict.TryGetValue("Size", out sSize);
            int.TryParse(sSize, out Size);

            string sTime;
            iTunesTrackDict.TryGetValue("Total Time", out sTime);
            int.TryParse(sTime, out Time);

            string sTrackNum;
            iTunesTrackDict.TryGetValue("Track Number", out sTrackNum);
            int.TryParse(sTime, out TrackNumber);

            string sBitRate;
            iTunesTrackDict.TryGetValue("Bit Rate", out sBitRate);
            int.TryParse(sBitRate, out BitRate);


            string sDateAdded;
            iTunesTrackDict.TryGetValue("Date Added", out sDateAdded);
            DateTime.TryParse(sDateAdded, out DateAdded);

            string sPath;
            iTunesTrackDict.TryGetValue("Location", out sPath);
            Ext = sPath.Substring(sPath.LastIndexOf('.') + 1, sPath.Length - sPath.LastIndexOf('.') - 1).ToLower();
            
        }

        public File()
        {
            FGuid = Guid.NewGuid();
            DateAdded = DateTime.Now;
        }
        public File(string name, string ext) : this()
        {
            Name = name;
            Ext = ext;
        }
        public File(string Path) : this()
        {
            string temp = Path;
            if (Path.Contains("/"))
                temp = Path.Substring(Path.LastIndexOf('/') + 1);
            else if (Path.Contains("\\"))
                temp = Path.Substring(Path.LastIndexOf('\\') + 1);

            if (temp.Contains("."))
            {
                Name = temp.Substring(0, temp.LastIndexOf('.'));
                Ext = temp.Substring(temp.LastIndexOf('.') + 1, temp.Length - temp.LastIndexOf('.') - 1).ToLower();
            }
            else
            {
                Name = temp;
                Ext = "";
            }
        }
        public bool isMusic()
        {
            if (!Set)
            {
                Set = true;
                Music = Ext != "" && Library.MusicExtensions.Contains(Ext);
                Artwork = Ext != "" && Library.ArtworkExtensions.Contains(Ext);
            }
            return Music;
        }
        public bool isImage()
        {
            if (!Set)
            {
                Set = true;
                Music = Ext != "" && (Ext.Equals("mp3") || Ext.Equals("wma") || Ext.Equals("m4a") || Ext.Equals("wav") || Ext.Equals("flac") ||
                                      Ext.Equals("ape") || Ext.Equals("aac") || Ext.Equals("atrac") || Ext.Equals("tta") || Ext.Equals("ogg"));
                Artwork = Ext != "" && (Ext.Equals("jpg") || Ext.Equals("jpeg") || Ext.Equals("png") || Ext.Equals("gif") || Ext.Equals("tif") || Ext.Equals("tiff"));
            }
            return Artwork;
        }

        protected File(SerializationInfo info, StreamingContext context)
        {
            Name = (string)info.GetString("Name");
            Ext = (string)info.GetString("Ext");
            FGuid = (Guid)info.GetValue("FGuid", typeof(Guid));
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", Name);
            info.AddValue("Ext", Ext);
            info.AddValue("FGuid", FGuid);
        }
        public int CompareTo(object fil)
        {
            return typeof(File) == fil.GetType() ? Name.CompareTo(((File)fil).Name) : -1;
        }
        public string FileName()
        {
            return Name + "." + Ext;
        }
        public override bool Equals(System.Object obj)
        {
            if (obj.GetType() != typeof(File)) return false;
            return Equals(obj as File);
        }
        public bool Equals(File f)
        {
            return FileName().Equals(f.FileName(), StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
