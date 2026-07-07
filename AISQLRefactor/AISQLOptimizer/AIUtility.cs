using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AISQLOptimizer
{
    internal class AIUtility
    {
        //Base folder for writing, valid on any machine: C:\Users\<utente>\AppData\Local\AISQLOptimizer
        private static string GetAppDataFolder()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AISQLOptimizer");
            Directory.CreateDirectory(folder);   // no-op se esiste già
            return folder;
        }

        public static void TraceLog(string stringToWrite)
        {
            try
            {
                string strFile = Path.Combine(GetAppDataFolder(), "AIDashboard.log");
                File.AppendAllText(strFile, DateTime.Now.ToString() + " : " + stringToWrite + Environment.NewLine);
            }
            catch
            {
                //Writings errors are ignored, the app must not crash because of logging.
            }
        }

        public static async Task<string> SaveHtmlToFileAsync(string htmlContent)
        {
            try
            {
                //Subfolder for html reports
                string folderPath = Path.Combine(GetAppDataFolder(), "Reports");
                Directory.CreateDirectory(folderPath);

                string fileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                string filePath = Path.Combine(folderPath, fileName);

                await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8);
                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il salvataggio HTML: {ex.Message}");
                throw;
            }
        }
    }
}