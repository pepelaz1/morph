using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using NGif;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Windows.Threading;
using System.Threading;

namespace MorphLib
{
    public class MorphBuilder
    {
        public Stream Outstream {get; set;}
        public int FrameRate {get;set;}
        public int FramesBetween { get; set; }
        public int OutputWidth { get; set; }
        public int OutputHeight { get; set; }
        public Boolean BackToOriginal { get; set; }
        public WriteableBitmap Source { get; set; }
        public WriteableBitmap Target { get; set; }
        public MorphGrid SourceGrid { get; set; }
        public MorphGrid TargetGrid { get; set; }
        
        private Point[,] _points;
        private ManualResetEvent _evnt = new ManualResetEvent(true);

        public MorphBuilder()
        {
            _evnt.Reset();
        }

        private void DoMorph(Dispatcher dispatcher, BackgroundWorker worker, AnimatedGifEncoder enc, int b, int e, int step)
        {
            int cnt = SourceGrid.Count;
            //for (int n = b; n < e; n+=step)
            int n = b;
            for (;;)
            {
               // worker.ReportProgress( (int)((double)n / double)e * 100) );
               // worker.ReportProgress((int)(((double)n / (double)e) * 100));
                 worker.ReportProgress((int)_progress);

                 _progress += _step;
                if (n == e)
                    break;

                // Get the original image's bounds.
                int ix_max = Source.PixelWidth - 2;
                int iy_max = Source.PixelHeight - 2;

                Point[,] pts = new Point[cnt, cnt];
                double fraction = (double)n / (double)FramesBetween;
                for (int i = 0; i < cnt; i++)
                {
                    for (int j = 0; j < cnt; j++)
                    {
                        Point pt = new Point();
                        pt.X = SourceGrid.Points[i, j].X * (1.0 - fraction) + _points[i, j].X * fraction;
                        pt.Y = SourceGrid.Points[i, j].Y * (1.0 - fraction) + _points[i, j].Y * fraction;
                        pts[i, j] = pt;
                    }
                }

                double s = 0;
                double t = 0;
                double x_in = 0;
                double y_in = 0;
                double r0 = 0;
                double g0 = 0;
                double b0 = 0;
                double r1 = 0;
                double g1 = 0;
                double b1 = 0;
                double dx1 = 0;
                double dx2 = 0;
                double dy1 = 0;
                double dy2 = 0;
                int v11 = 0;
                int v12 = 0;
                int v21 = 0;
                int v22 = 0;
                int ix_in = 0;
                int iy_in = 0;

                WriteableBitmap bmp = null;
                _evnt.Reset();
                dispatcher.BeginInvoke(() =>
                {
                     bmp = new WriteableBitmap(Source.PixelWidth, Source.PixelHeight);
                     _evnt.Set();
                });
                //while(bmp == null)
                   //Thread.Sleep(1);
                _evnt.WaitOne();

                    for (int iy_out = 0; iy_out <= iy_max; iy_out++)
                    {
                        for (int ix_out = 0; ix_out <= ix_max; ix_out++)
                        {
                            // Find the row and column in the current
                            // grid that contains this point.
                            bool found_grid = false;
                            int row = 0;
                            int col = 0;
                            for (row = 0; row <= cnt - 2; row++)
                            {
                                for (col = 0; col <= cnt - 2; col++)
                                {
                                    if (PointsToST(ix_out, iy_out, ref s, ref t, pts[row, col].X, pts[row, col].Y,
                                        pts[row, col + 1].X, pts[row, col + 1].Y, pts[row + 1, col].X, pts[row + 1, col].Y,
                                        pts[row + 1, col + 1].X, pts[row + 1, col + 1].Y))
                                    {
                                        // The point is in this grid.
                                        found_grid = true;
                                        break; // TODO: might not be correct. Was : Exit For
                                    }
                                }
                                if (found_grid)
                                    break; // TODO: might not be correct. Was : Exit For
                            }

                            if (found_grid)
                            {
                                // Find the corresponding points in m_Pic(0).
                                STToPoints(ref x_in, ref y_in, s, t, SourceGrid.Points[row, col].X, SourceGrid.Points[row, col].Y, SourceGrid.Points[row, col + 1].X,
                                    SourceGrid.Points[row, col + 1].Y, SourceGrid.Points[row + 1, col].X, SourceGrid.Points[row + 1, col].Y,
                                    SourceGrid.Points[row + 1, col + 1].X, SourceGrid.Points[row + 1, col + 1].Y);

                                // Interpolate to find the pixel's value.
                                // Find the nearest integral position.
                                ix_in = (int)x_in;
                                iy_in = (int)y_in; 
                              

                                // See if this is out of bounds.
                                if ((ix_in < 0) || (ix_in > ix_max) | (iy_in < 0) || (iy_in > iy_max))
                                {
                                    // The point is outside the image.
                                    // Use black.
                                    r0 = 0;
                                    g0 = 0;
                                    b0 = 0;
                                }
                                else
                                {
                                    // The point lies within the image.
                                    // Calculate its value.
                                    dx1 = x_in - ix_in;
                                    dy1 = y_in - iy_in;
                                    dx2 = 1.0 - dx1;
                                    dy2 = 1.0 - dy1;

                                    // Calculate the red value.
                                    v11 = Source.GetPixel(ix_in, iy_in).R;
                                    v12 = Source.GetPixel(ix_in, iy_in + 1).R;
                                    v21 = Source.GetPixel(ix_in + 1, iy_in).R;
                                    v22 = Source.GetPixel(ix_in + 1, iy_in + 1).R;

                                    r0 = v11 * dx2 * dy2 + v12 * dx2 * dy1 + v21 * dx1 * dy2 + v22 * dx1 * dy1;
                                    
                                    // Calculate the green value.
                                    v11 = Source.GetPixel(ix_in, iy_in).G;
                                    v12 = Source.GetPixel(ix_in, iy_in + 1).G;
                                    v21 = Source.GetPixel(ix_in + 1, iy_in).G;
                                    v22 = Source.GetPixel(ix_in + 1, iy_in + 1).G;

                                    g0 = v11 * dx2 * dy2 + v12 * dx2 * dy1 + v21 * dx1 * dy2 + v22 * dx1 * dy1;

                                    // Calculate the blue value.
                                    v11 = Source.GetPixel(ix_in, iy_in).B;
                                    v12 = Source.GetPixel(ix_in, iy_in + 1).B;
                                    v21 = Source.GetPixel(ix_in + 1, iy_in).B;
                                    v22 = Source.GetPixel(ix_in + 1, iy_in + 1).B;

                                    b0 = v11 * dx2 * dy2 + v12 * dx2 * dy1 + v21 * dx1 * dy2 + v22 * dx1 * dy1;


                                    if (Math.Abs(r0 - 255) < 5 && Math.Abs(g0 - 255) < 5 && Math.Abs(b0 - 255) < 5)
                                    {
                                        int tt = 4;
                                        tt = 4;
                                    }
                                }

                                // Find the corresponding points in m_Pic(1).
                                STToPoints(ref x_in, ref y_in, s, t, _points[row, col].X, _points[row, col].Y,
                                    _points[row, col + 1].X, _points[row, col + 1].Y, _points[row + 1, col].X,
                                    _points[row + 1, col].Y, _points[row + 1, col + 1].X, _points[row + 1, col + 1].Y);

                                // Interpolate to find the pixel's value.
                                // Find the nearest integral position.
                                ix_in = (int)x_in;
                                iy_in = (int)y_in; 

                                // See if this is out of bounds.
                                if ((ix_in < 0) | (ix_in > ix_max) | (iy_in < 0) | (iy_in > iy_max))
                                {
                                    // The point is outside the image.
                                    // Use black.
                                    r1 = 0;
                                    g1 = 0;
                                    b1 = 0;
                                }
                                else
                                {
                                    // The point lies within the image.
                                    // Calculate its value.
                                    dx1 = x_in - ix_in;
                                    dy1 = y_in - iy_in;
                                    dx2 = 1.0 - dx1;
                                    dy2 = 1.0 - dy1;

                                    // Calculate the red value.
                                    v11 = Target.GetPixel(ix_in, iy_in).R;
                                    v12 = Target.GetPixel(ix_in, iy_in + 1).R;
                                    v21 = Target.GetPixel(ix_in + 1, iy_in).R;
                                    v22 = Target.GetPixel(ix_in + 1, iy_in + 1).R;

                                    r1 = v11 * dx2 * dy2 + v12 * dx2 * dy1 + v21 * dx1 * dy2 + v22 * dx1 * dy1;

                                    // Calculate the green value.
                                    v11 = Target.GetPixel(ix_in, iy_in).G;
                                    v12 = Target.GetPixel(ix_in, iy_in + 1).G;
                                    v21 = Target.GetPixel(ix_in + 1, iy_in).G;
                                    v22 = Target.GetPixel(ix_in + 1, iy_in + 1).G;

                                    g1 = v11 * dx2 * dy2 + v12 * dx2 * dy1 + v21 * dx1 * dy2 + v22 * dx1 * dy1;

                                    // Calculate the blue value.
                                    v11 = Target.GetPixel(ix_in, iy_in).B;
                                    v12 = Target.GetPixel(ix_in, iy_in + 1).B;
                                    v21 = Target.GetPixel(ix_in + 1, iy_in).B;
                                    v22 = Target.GetPixel(ix_in + 1, iy_in + 1).B;

                                    b1 = v11 * dx2 * dy2 + v12 * dx2 * dy1 + v21 * dx1 * dy2 + v22 * dx1 * dy1;

                                }
                                // Combine the values of the two colors.
                                byte rr = (byte)(r0 * (1.0 - fraction) + r1 * fraction);
                                byte gg = (byte)(g0 * (1.0 - fraction) + g1 * fraction);
                                byte bb = (byte)(b0 * (1.0 - fraction) + b1 * fraction);
                                bmp.SetPixel(ix_out, iy_out, Color.FromArgb(255,rr,gg,bb));

                            }
                            else
                            {
                                bmp.SetPixel(ix_out, iy_out, Colors.Red);
                            }
                            // End if found_grid ...
                        }
                    }
                    _evnt.Reset();
                    dispatcher.BeginInvoke(() =>
                    {
                      enc.AddFrame(dispatcher, bmp);
                      _evnt.Set();
                    });

                    _evnt.WaitOne();
                    //Thread.Sleep(10);
                    enc.SetDelay(1000 / FrameRate);
                    //    if (tt++ > 2) 
                    //     break;
               
                n += step;
            }
        }

        double _progress;
        double _step;

        public void Process(Dispatcher dispatcher, BackgroundWorker worker)
        {
            try
            {
                AnimatedGifEncoder enc = new AnimatedGifEncoder();
                enc.Start(Outstream);
             //   enc.SetDelay(25);   // 1 frame per sec
                enc.SetRepeat(0);
               // enc.SetDelay(1000.0 / FrameRate);
                enc.SetSize(OutputWidth, OutputHeight);

                double kx = (double)(Source.PixelWidth) / (double)(Target.PixelWidth);
                double ky = (double)(Source.PixelHeight) / (double)(Target.PixelHeight);

                _points = new Point[SourceGrid.Count, SourceGrid.Count];
                
                for (int i = 0; i < SourceGrid.Count; i++)
                {
                    for (int j = 0; j < SourceGrid.Count; j++)
                    {
                        Point p = new Point(TargetGrid.Points[i, j].X * kx, TargetGrid.Points[i, j].Y * ky);
                        _points[i, j] = p;
                     }
                }

                // Make target image same size as source
                _evnt.Reset();
                dispatcher.BeginInvoke(() =>
                {
                    Target = Target.Resize(Source.PixelWidth, Source.PixelHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
                    _evnt.Set();
                });

                _evnt.WaitOne();

                

            //    byte[] src = WriteableBitmapExtensions.ToByteArray(Source);
            //    byte[] dst = WriteableBitmapExtensions.ToByteArray(Target);

               // int len = src.Length > dst.Length ? dst.Length : src.Length;

           //     byte[] mid = new byte[len];

                _progress = 0;
                _step = BackToOriginal ? 100.0/((double)FramesBetween*2) : 100.0/(double)FramesBetween;

                DoMorph(dispatcher, worker, enc, 0, FramesBetween, 1);
                if (BackToOriginal)
                    DoMorph(dispatcher, worker, enc, FramesBetween, 0, -1);
             
                enc.Finish();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        // Find S and T for the point (X, Y) in the
        // quadrilateral with points (x1, y1), (x2, y2),
        // (x3, y3), and (x4, y4). Return True if the point
        // lies within the quadrilateral and False otherwise.
        private bool PointsToST(double X, double Y, ref double s, ref double t, double x1, double y1, double x2, double y2, double x3, double y3,  double x4, double y4)
        {
            double Ax = 0;
            double Bx = 0;
            double Cx = 0;
            double Dx = 0;
            double Ex = 0;
            double Ay = 0;
            double By = 0;
            double Cy = 0;
            double Dy = 0;
            double Ey = 0;
            double a = 0;
            double b = 0;
            double c = 0;
            double det = 0;
            double denom = 0;

            Ax = x2 - x1;
            Ay = y2 - y1;
            Bx = x4 - x3;
            By = y4 - y3;
            Cx = x3 - x1;
            Cy = y3 - y1;
            Dx = X - x1;
            Dy = Y - y1;
            Ex = Bx - Ax;
            Ey = By - Ay;

            a = -Ax * Ey + Ay * Ex;
            b = Ey * Dx - Dy * Ex + Ay * Cx - Ax * Cy;
            c = Dx * Cy - Dy * Cx;

            det = b * b - 4 * a * c;
            if (det >= 0)
            {
                if (Math.Abs(a) < 0.001)
                {
                    t = -c / b;
                }
                else
                {
                    t = (-b - Math.Sqrt(det)) / (2 * a);
                }
                denom = (Cx + Ex * t);
                if (Math.Abs(denom) > 0.001)
                {
                    s = (Dx - Ax * t) / denom;
                }
                else
                {
                    denom = (Cy + Ey * t);
                    if (Math.Abs(denom) > 0.001)
                    {
                        s = (Dy - Ay * t) / denom;
                    }
                    else
                    {
                        s = -1;
                    }
                }

                return (t >= -1E-05 & t <= 1.00001 & s >= -1E-05 & s <= 1.00001);
            }
            else
            {
                return false;
            }
        }

        // Using s and t values, return the coordinates of a
        // point in a quadrilateral.
        private void STToPoints(ref double X, ref double Y, double s, double t, double x1, double y1, double x2, double y2, double x3, double y3,  double x4, double y4)
        {
            double xa = 0;
            double ya = 0;
            double xb = 0;
            double yb = 0;

            xa = x1 + t * (x2 - x1);
            ya = y1 + t * (y2 - y1);
            xb = x3 + t * (x4 - x3);
            yb = y3 + t * (y4 - y3);
            X = xa + s * (xb - xa);
            Y = ya + s * (yb - ya);
        }
    }
}
