using Microsoft.Win32;
using System.Diagnostics.Tracing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CysTem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        String[] FilePaths = Array.Empty<string>();
        String[] FileNames = Array.Empty<string>();
        String[] FileExtensions = Array.Empty<string>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Original_File_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.FileName = "";
            ofd.Multiselect = true;
            ofd.Filter = "Sound effect file (*.k0)|*.k0|Voice file (*.j0)|*.j0|OGG Vorbis file (*.ogg)|*.ogg|Background music file (*.u0)|*.u0|All files (*.*)|*.*";
            ofd.FilterIndex = ofd.Filter.Length;
            Nullable<bool> result = ofd.ShowDialog();

            if (result == true)
            {
                try
                {
                    //Copy the values for the selected files to an array in order to manage the
                    //files later on according to their extension
                    FilePaths = (string[])ofd.FileNames.Clone();
                    FileNames = new string[FilePaths.Length];
                    FileExtensions = new string[FilePaths.Length];
                    for (int CurrentFile = 0; CurrentFile < ofd.FileNames.Length; CurrentFile++)
                    {
                        FileNames[CurrentFile] = System.IO.Path.GetFileNameWithoutExtension(ofd.FileNames[CurrentFile]);
                        FileExtensions[CurrentFile] = System.IO.Path.GetExtension(FilePaths[CurrentFile]);
                    }

                    Button_Convert.IsEnabled = false;

                    for (int CurrentFile = 0; CurrentFile < FilePaths.Length; CurrentFile++)
                    {
                        //Check to see if the file is one that is not compatible with the program
                        if (FileExtensions[CurrentFile] != ".ogg" && FileExtensions[CurrentFile] != ".k0" && FileExtensions[CurrentFile] != ".j0"
                            && FileExtensions[CurrentFile] != ".u0")
                        {
                            MessageBox.Show($"At least one selected file is not compatible with the program, and said files" +
                                $" will not be processed.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            break;
                        }
                    }

                    //Check if there are compatible files selected, so that way we know if we can enable the convert button or not.
                    //This loop is done again on a separate loop just in the edge case the user's first file happens to be an
                    //incompatible file but has chosen compatible files too
                    for (int CurrentFile = 0; CurrentFile < FilePaths.Length; CurrentFile++)
                    {
                        //We see that there is a compatible file selected, so we enable the button
                        if (FileExtensions[CurrentFile] == ".ogg" || FileExtensions[CurrentFile] == ".k0" || FileExtensions[CurrentFile] == ".j0"
                            || FileExtensions[CurrentFile] == ".u0")
                        {
                            Button_Convert.IsEnabled = true;
                            break;
                        }
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Button_Convert_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog ofd = new OpenFolderDialog();
            Nullable<bool> result = ofd.ShowDialog();

            if (result == true)
            {
                try
                {
                    //We start a StringBuilder in case we have to unpack files, so we can store its metadata
                    //in a separate file
                    StringBuilder sb = new StringBuilder();

                    for (int CurrentFile = 0; CurrentFile < FilePaths.Length; CurrentFile++)
                    {
                        //We check what extension the current file has
                        switch (FileExtensions[CurrentFile])
                        {
                            case ".k0":
                            case ".j0":
                            case ".u0":
                                {
                                    Ogg ogg = new Ogg(FilePaths[CurrentFile], "", 0);
                                    File.WriteAllBytes(ofd.FolderName + "\\" + FileNames[CurrentFile] + ".ogg", ogg.Decompile());
                                    sb.AppendLine("File: " + FileNames[CurrentFile] + FileExtensions[CurrentFile]);
                                    string[] Metadata = ogg.GetMetadata();
                                    sb.AppendLine("Key: " + Metadata[0]);
                                    sb.AppendLine("Extra offset displacement: " + Metadata[1]);
                                    sb.AppendLine();
                                    break;
                                }
                            case ".ogg":
                                {
                                    if (File.Exists(System.IO.Path.GetDirectoryName(FilePaths[CurrentFile]) + "\\" + 
                                        "Metadata.txt"))
                                    {
                                        var Lines = File.ReadAllLines(System.IO.Path.GetDirectoryName(FilePaths[CurrentFile]) + "\\" +
                                        "Metadata.txt", Encoding.ASCII);
                                        for (var CurrentLine = 0; CurrentLine < Lines.Length; CurrentLine++)
                                        {
                                            if (Lines[CurrentLine].StartsWith("File: " + FileNames[CurrentFile]))
                                            {
                                                string Extension = Lines[CurrentLine].Split('.')[1];
                                                string Key = Lines[CurrentLine + 1].Replace("Key: ", "");
                                                int ExtraOffsetDisplacement = Convert.ToInt32(Lines[CurrentLine + 2].Split(':')[1]);
                                                Ogg ogg = new Ogg(FilePaths[CurrentFile], Key, ExtraOffsetDisplacement);
                                                File.WriteAllBytes(ofd.FolderName + "\\" + FileNames[CurrentFile] + "." + Extension, ogg.Compile());
                                                break;
                                            }

                                            CurrentLine += 3;
                                        }
                                        
                                    }
                                    else
                                    {
                                        throw new Exception("The metadata file for the reconstruction of the compressed files" +
                                            " is missing, make sure to regenerate it by uncompressing again the original files");
                                    }
                                    break;
                                }
                        }
                    }

                    if (sb.Length > 4)
                    {
                        //Remove the last /r/n characters from the string in order to take out the last empty line
                        sb.Remove(sb.Length - 4, 4);
                        File.WriteAllText(ofd.FolderName + "\\" + "Metadata.txt", sb.ToString());
                    }
                    Button_Convert.IsEnabled = false;
                    MessageBox.Show($"Process completed successfully.", "Conversion completed.", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}