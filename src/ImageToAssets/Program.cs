using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace ImageToAssets;

public sealed class Program {
  public static int Main(string[] args) {
    if (args.Length != 2) {
      Console.WriteLine("Missing arguments: ita source destination");
      return -1;
    }

    var source = args[0];
    var destination = args[1];
    var tempDestination = Path.GetTempFileName() + ".img";

    Directory.CreateDirectory(tempDestination);
    Console.WriteLine("Source: " + source);
    Console.WriteLine("Destination: " + destination);
    Console.WriteLine("Temp destination: " + tempDestination);

    var sourceFiles = Directory.GetFiles(source, "*.*", new EnumerationOptions { RecurseSubdirectories = true });

    Parallel.ForEach(sourceFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (sourceFile) => ConvertImage(sourceFile, tempDestination));

    if (Directory.Exists(destination)) {
      Directory.Delete(destination, true);
    }

    var tempFiles = Directory.GetFiles(tempDestination, "*.*", new EnumerationOptions { RecurseSubdirectories = true });

    foreach (var file in tempFiles) {
      var fileName = file[(tempDestination.Length + 1)..];
      string copyDirectory;

      if (fileName.Contains(Path.DirectorySeparatorChar)) {
        copyDirectory = Path.Combine(destination, Path.GetDirectoryName(fileName)!);
      } else {
        copyDirectory = destination;
      }

      if (Directory.Exists(copyDirectory) == false) {
        Directory.CreateDirectory(copyDirectory);
      }

      File.Copy(file, Path.Combine(copyDirectory, Path.GetFileName(file)), true);
    }

    Directory.Delete(tempDestination, true);
    return 0;
  }

  private static void ConvertImage(string sourceFile, string destination) {
    try {
      Console.WriteLine(sourceFile);

      using var image = Image.Load(sourceFile);

      float[] sizes = [2.0f, 1.0f, 3.0f, 1.75f, 2.625f, 2.75f, 1.5f, 4.0f];

      Parallel.ForEach(sizes, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (scale, cancellationToken) => {
        var clone = image.Clone((context) => ResizeImage(context, scale));
        var destinationFolder = scale == 1.0f ? destination : Path.Combine(destination, scale.ToString() + "x");

        Directory.CreateDirectory(destinationFolder);

        clone.SaveAsWebp(Path.Combine(destinationFolder, Path.GetFileNameWithoutExtension(sourceFile) + ".webp"), new WebpEncoder {
          Method = WebpEncodingMethod.Level6,
          NearLossless = true,
          SkipMetadata = true,
        });
      });
    } catch (Exception ex) {
      Console.WriteLine(sourceFile + "\n" + ex.Message);
    }
  }

  private static void ResizeImage(IImageProcessingContext context, float scale) {
    var imageSize = context.GetCurrentSize();

    scale /= 4.0f;
    context.Resize(Convert.ToInt32(imageSize.Width * scale), Convert.ToInt32(imageSize.Height * scale), new TriangleResampler());
  }
}
