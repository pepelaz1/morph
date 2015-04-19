using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Browser;
using System.Diagnostics;
using MorphLib;
using System.Text;
using System.Globalization;
using System.ComponentModel;
using System.Threading;

namespace Morph
{
    public partial class MainPage : UserControl
    {
        private MorphGrid _grid1, _grid2;

        private BackgroundWorker _bw = new BackgroundWorker();
        private MorphBuilder _mb;
        private WriteableBitmap _src, _dst;

        public MainPage()
        {
            InitializeComponent();

            _bw.WorkerReportsProgress = true;
            _bw.WorkerSupportsCancellation = true;
            _bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            _bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            _bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
        }

        private void btnLoad1_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image files (*.jpg)|*.jpg";
            ofd.FilterIndex = 1;

            if ((bool)ofd.ShowDialog())
            {
                try
                {                    
                    using (Stream s = ofd.File.OpenRead())
                    {
                        BitmapImage bmp = new BitmapImage();
                        bmp.SetSource(s);
                        image1.Source = bmp;

                        canvas1.Width = image1.ActualWidth;
                        canvas1.Height = image1.ActualHeight;

                        canvas1.Children.Clear();
                    //    canvas2.Children.Clear();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btnLoad2_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image files (*.jpg)|*.jpg";
            ofd.FilterIndex = 1;

            if ((bool)ofd.ShowDialog())
            {
                try
                {
                    using (Stream s = ofd.File.OpenRead())
                    {
                        BitmapImage bmp = new BitmapImage();
                        bmp.SetSource(s);
                        image2.Source = bmp;

                        canvas2.Width = image2.ActualWidth;
                        canvas2.Height = image2.ActualHeight;

                  //      canvas1.Children.Clear();
                        canvas2.Children.Clear();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }       


    

        private void image1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            canvas1.Width = e.NewSize.Width;
            canvas1.Height = e.NewSize.Height;
       }

        private void image2_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            canvas2.Width = e.NewSize.Width;
            canvas2.Height = e.NewSize.Height;
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
           // if (_bw.IsBusy != true)
           //// {
           //     _bw.RunWorkerAsync();
           // }
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = ".gif";
            sfd.Filter = "GIF files (*.gif)|*.gif";
            sfd.FilterIndex = 1;
            if (sfd.ShowDialog() == true)
            {
                //  using (Stream strm = sfd.OpenFile())
                //  {


                try
                {
                    _mb = new MorphBuilder();
                    _mb.Outstream = sfd.OpenFile();
                    
                    _mb.FrameRate = (int)udFrameRate.Value;

                    _mb.FramesBetween = (int)udFramesBetween.Value;
                    _mb.OutputWidth = (int)udWidth.Value;
                    _mb.OutputHeight = (int)udHeight.Value;
                    _mb.BackToOriginal = (bool)cbBack.IsChecked;
                    _mb.Source = new WriteableBitmap(image1, null);
                    _mb.Target = new WriteableBitmap(image2, null);
                    _mb.SourceGrid = _grid1;
                    _mb.TargetGrid = _grid2;
                    if (_bw.IsBusy != true)
                    {
                        tbStatus.Text = "Working";
                        _bw.RunWorkerAsync();
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                  }               
            }
        }

        private void image1_ImageOpened(object sender, RoutedEventArgs e)
        {
            _grid1 = new MorphGrid(canvas1, (int)udGridCount.Value);
            _grid1.Draw();
        }

        private void image2_ImageOpened(object sender, RoutedEventArgs e)
        {
            _grid2 = new MorphGrid(canvas2, (int)udGridCount.Value);
            _grid2.Draw();
        }

        private void btnLoadGrid_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Morph grid files (*.grid)|*.grid";
            ofd.FilterIndex = 1;

            if ((bool)ofd.ShowDialog())
            {
                try
                {
                    using (var strm = ofd.File.OpenText())
                    {
                        String s = strm.ReadLine();
                       // MessageBox.Show(s);
                        String []parts = s.Split(" ".ToCharArray());
                        int cnt = int.Parse(parts[0]);
                        udGridCount.Value = cnt;
                        int n = 1;
                        _grid1 = new MorphGrid(canvas1, cnt);
                        _grid2 = new MorphGrid(canvas2, cnt);
                        for (int i = 0; i < cnt; i++)
                        {
                            for (int j = 0; j < cnt; j++)
                            {
                                _grid1.Points[i, j].X = double.Parse(parts[n++], CultureInfo.InvariantCulture);
                                _grid1.Points[i, j].Y = double.Parse(parts[n++], CultureInfo.InvariantCulture);
                                _grid2.Points[i, j].X = double.Parse(parts[n++], CultureInfo.InvariantCulture);
                                _grid2.Points[i, j].Y = double.Parse(parts[n++], CultureInfo.InvariantCulture);
                            }
                        }
                        _grid1.Draw();
                        _grid2.Draw();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btnSaveGrid_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = ".gif";
            sfd.Filter = "Morph grid files (*.grid)|*.grid";
            sfd.FilterIndex = 1;
            if (sfd.ShowDialog() == true)
            {
                using ( var strm = sfd.OpenFile())
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append( ((int)udGridCount.Value).ToString());
                    sb.Append(" ");

                    for (int i = 0; i < (int)udGridCount.Value; i++)
                    {
                        for (int j = 0; j < (int)udGridCount.Value; j++)
                        {
                            sb.Append((_grid1.Points[i, j].X.ToString(CultureInfo.InvariantCulture)));
                            sb.Append(" ");
                            sb.Append((_grid1.Points[i, j].Y.ToString(CultureInfo.InvariantCulture)));
                            sb.Append(" ");
                            sb.Append((_grid2.Points[i, j].X.ToString(CultureInfo.InvariantCulture)));
                            sb.Append(" ");
                            sb.Append((_grid2.Points[i, j].Y.ToString(CultureInfo.InvariantCulture)));
                            sb.Append(" ");
                        }
                    }
                    byte[] a = (new UTF8Encoding(true)).GetBytes(sb.ToString());
                    strm.Write(a, 0, a.Length);
                    strm.Close(); 
                }
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_bw.WorkerSupportsCancellation == true)
            {
                _bw.CancelAsync();
            }
        }
        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
             _mb.Process(Dispatcher, worker);
           }
        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((e.Cancelled == true))
            {
              //  this.tbProgress.Text = "Canceled!";
                //MessageBox.Show("Canceled");
                tbStatus.Text = "Canceled";
            }

            else if (!(e.Error == null))
            {
              //  this.tbProgress.Text = ("Error: " + e.Error.Message);
                //MessageBox.Show("Error");
                tbStatus.Text = "Error";
                MessageBox.Show(e.Error.Message);
            }

            else
            {
                // MessageBox.Show("Ok");
               // this.tbProgress.Text = "Done!";
                tbStatus.Text = "Finished";
                _mb.Outstream.Close();
            }
        }
        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                progressProcess.Value = e.ProgressPercentage;
            });
           // this.tbProgress.Text = (e.ProgressPercentage.ToString() + "%");
        }
        
 
    }
}
