// Decompiled with JetBrains decompiler
// Type: Gw2_Launchbuddy.Helpers.ScreenAnalyser
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Collections.Generic;
using System.Drawing;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace Gw2_Launchbuddy.Helpers
{
    public static class ScreenAnalyser
    {
        public static List<Point> FindElements(
          Bitmap srcbitmap,
          Bitmap templatebitmap,
          double scaling = 1.0,
          Rect roi = default(Rect))
        {
            return ScreenAnalyser.FindElements(
                srcbitmap, 
                new Bitmap[1] 
                {
                    templatebitmap
                }, 
                scaling, 
                roi);
        }

        public static List<Point> FindElements(
          Bitmap srcbitmap,
          Bitmap[] templatebitmaps,
          double scaling = 1.0,
          Rect roi = default(Rect),
          bool showmatch = false)
        {
            List<Point> elements = new List<Point>();
            if (srcbitmap == null)
                return new List<Point>();

            Mat mat1 = BitmapConverter.ToMat(srcbitmap);
            Cv2.CvtColor(InputArray.Create(mat1), OutputArray.Create(mat1), (ColorConversionCodes)10, 0);
            Cv2.Threshold(InputArray.Create(mat1), OutputArray.Create(mat1), 180.0, 200.0, (ThresholdTypes)3);
            double num1 = 1.0 / scaling;
            Mat mat2 = mat1.Resize(Size.Zero, num1, num1, (InterpolationFlags)1);
            if (!roi.Equals(Rect.Empty))
            {
                try
                {
                    mat2 = new Mat(mat2, roi);
                }
                catch (OpenCVException ex)
                {
                    return new List<Point>();
                }
            }
            if (mat2.CountNonZero() == 0)
                return new List<Point>();
            ((DisposableObject)mat1).Dispose();
            foreach (Bitmap templatebitmap in templatebitmaps)
            {
                Mat mat3 = BitmapConverter.ToMat(templatebitmap);
                Cv2.CvtColor(mat3, mat3, (ColorConversionCodes)10, 0);
                Cv2.Threshold(mat3, mat3, 180.0, 200.0, (ThresholdTypes)3);
                using (Mat mat4 = new Mat(mat2.Rows - mat2.Rows + 1, mat2.Cols - mat2.Cols + 1, MatType.CV_32FC1))
                {
                    Cv2.MatchTemplate(mat2, mat3, mat4, (TemplateMatchModes)5, (InputArray)null);
                    Cv2.Threshold(mat4, mat4, 0.9, 1.0, (ThresholdTypes)3);
                    while (true)
                    {
                        double num2 = 0.9;
                        double num3;
                        double num4;
                        Point point1;
                        Point point2;
                        Cv2.MinMaxLoc(mat4, out num3, out num4, out point1, out point2);
                        if (num4 >= num2)
                        {
                            elements.Add(new Point(point2.X + mat3.Width / 2, point2.Y + mat3.Height / 2));
                            Rect rect;
                            Cv2.FloodFill(mat4, point2, new Scalar(0.0), out rect, new Scalar?(new Scalar(0.1)), new Scalar?(new Scalar(1.0)), (FloodFillFlags)4);
                        }
                        else
                            break;
                    }
                }
              ((DisposableObject)mat3).Dispose();
            }
          ((DisposableObject)mat2).Dispose();
            return elements;
        }


        public static bool DoesTemplateExists(Bitmap screenshotBitmap, Bitmap templateBitmap)
        {
            Mat screenshot = BitmapConverter.ToMat(screenshotBitmap);
            Mat template = BitmapConverter.ToMat(templateBitmap);
            Mat screenshotGray = new Mat();
            Mat templateGray = new Mat();
            Mat result = new Mat();

            Cv2.CvtColor(screenshot, screenshotGray, ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(template, templateGray, ColorConversionCodes.BGR2GRAY);

            Cv2.MatchTemplate(screenshotGray, templateGray, result, TemplateMatchModes.CCoeffNormed);

            double threshold = 0.8;
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);

            return maxVal >= threshold;
            
        }
    }
}
