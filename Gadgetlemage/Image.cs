using System;
using System.IO;

namespace Gadgetlemage
{
    class Image
    {
        /// <summary>
        /// Changes destination image with automatic image.
        /// </summary>
        /// <returns></returns>
        static public void changeToAutomaticImage()
        {
            if (!Properties.Settings.Default.Image) return;

            string destPath = Properties.Settings.Default.ImageDestinationPath;
            string autoPath = Properties.Settings.Default.ImageAutomaticPath;

            if (autoPath == "none" || destPath == "none") return;

            if (File.Exists(autoPath))
            {
#if DEBUG
                Console.WriteLine("Copying automatic image to destination path!");
#endif
                File.Copy(autoPath, destPath, true);
            }
        }

        /// <summary>
        /// Changes destination image with manual image.
        /// </summary>
        /// <returns></returns>
        static public void changeToManualImage()
        {
            if (!Properties.Settings.Default.Image) return;

            string destPath = Properties.Settings.Default.ImageDestinationPath;
            string manPath = Properties.Settings.Default.ImageManualPath;

            if (manPath == "none" || destPath == "none") return;

            if (File.Exists(manPath))
            {
#if DEBUG
                Console.WriteLine("Copying manual image to destination path!");
#endif
                File.Copy(manPath, destPath, true);
            }
        }

        /// <summary>
        /// Removes destination image.
        /// </summary>
        /// <returns></returns>
        static public void clearImage()
        {
            if (!Properties.Settings.Default.Image) return;

            string path = Properties.Settings.Default.ImageDestinationPath;

            if (path == "none") return;
            if (File.Exists(path))
            {
#if DEBUG
                Console.WriteLine("Removing destination path!");
#endif
                File.Delete(path);
            }
        }
    }
}
