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
using System.Collections.Generic;

namespace MorphLib
{
    class Pair
    {
        public Pair(int r_, int c_)
        {
            r = r_;
            c = c_;
        }

        public int r;
        public int c;
    };

    public class MorphGrid
    {
        private Canvas _canvas;
        private int _cnt;

        public int Count
        {
            get { return _cnt; }
        }

        private Ellipse _el;   
        private Point[,] _points;

        public Point[,] Points
        {
            get { return _points; }
        }

        private List<Line> _lines = new List<Line>();

        public MorphGrid(Canvas canvas, int cnt)
        {
            _canvas = canvas;
            _canvas.Children.Clear();
            _cnt = cnt;
            

            Generate();
        }

        private void Generate()
        {
            _points = null;
            _points = new Point[_cnt, _cnt];
            
            double w = _canvas.Width;
            double h = _canvas.Height;
            double dx = (double)(w-1) / (double)(_cnt-1);
            double dy = (double)(h-1) / (double)(_cnt-1);

            double y = 0;
            for (int r = 0; r < _cnt; r++ )
            {
                double x = 0;
                for (int c = 0; c < _cnt; c++)
                {
                    Point p = new Point(x, y);
                    _points[r, c] = p;
                    x += dx;
                }
                y += dy;
            }
        }

        public void Draw()
        {
            for (int r = 0; r < _cnt; r++)
            {
                for (int c = 0; c < _cnt; c++)
                {
                    if (r > 0)
                    {
                        Line line = new Line();
                        line.X1 = _points[r, c].X;
                        line.Y1 = _points[r, c].Y;
                        line.X2 = _points[r-1, c].X;
                        line.Y2 = _points[r-1, c].Y;
                        line.Stroke = new SolidColorBrush(Colors.White);
                        _canvas.Children.Add(line);
                    }
                    if (c > 0)
                    {
                        Line line = new Line();
                        line.X1 = _points[r, c].X;
                        line.Y1 = _points[r, c].Y;
                        line.X2 = _points[r, c-1].X;
                        line.Y2 = _points[r, c-1].Y;
                        line.Stroke = new SolidColorBrush(Colors.White);
                        _canvas.Children.Add(line);
                    }
                }
            }

            for (int r = 0; r < _cnt; r++)
            {
                for (int c = 0; c < _cnt; c++)
                {
                    Ellipse el = new Ellipse();
                    el.Width = 6;
                    el.Height = 6;
                    el.Stroke = new SolidColorBrush(Colors.White);
                    el.Fill = new SolidColorBrush(Colors.Black);
                    el.MouseLeftButtonDown += new MouseButtonEventHandler(el_MouseLeftButtonDown);
                    el.MouseMove += new MouseEventHandler(el_MouseMove);
                    el.MouseLeftButtonUp += new MouseButtonEventHandler(el_MouseLeftButtonUp);
                    el.Cursor = Cursors.Hand;

                    /*List<Point> pts = new List<Point>();
                    pts.Add(_points[r,c]);
                    if (r > 0) pts.Add( _points[r-1,c] );
                    if (c > 0) pts.Add( _points[r,c-1] );
                    if (r < _cnt - 1) pts.Add(_points[r + 1, c]);
                    if (c < _cnt - 1) pts.Add(_points[r, c+1]);
                    el.Tag = pts;*/

                    el.Tag = new Pair(r, c);

                    _canvas.Children.Add(el);
                    Canvas.SetLeft(el, _points[r,c].X-3);
                    Canvas.SetTop(el, _points[r, c].Y-3);
                }
            }
        }

        //public void ChangePoints(Point[,] points)
        //{
        //    for (int r = 0; r < _cnt; r++)
        //    {
        //        for (int c = 0; c < _cnt; c++)
        //        {
        //            _points[r, c].X = points[r, c].X;
        //            _points[r, c].Y = points[r, c].Y;
        //        }
        //    }
        //}

        private void el_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _el = sender as Ellipse;
                _el.CaptureMouse();

                //Point pt = (Point)_el.Tag;
                Pair p = (Pair)_el.Tag;
                _lines.Clear();
                foreach (UIElement element in _canvas.Children)
                {
                    if (element is Line)
                    {
                        Line l = element as Line;
                        if (Math.Abs(l.X1 - _points[p.r, p.c].X) < 0.01 && Math.Abs(l.Y1 - _points[p.r, p.c].Y) < 0.01 || Math.Abs(l.X2 - _points[p.r, p.c].X) < 0.01 && Math.Abs(l.Y2 - _points[p.r, p.c].Y) < 0.01)
                            _lines.Add(l);

                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
        }

        private void el_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {

                if (_el != null)
                {
                    Point current = e.GetPosition((UIElement)_canvas);
                    Pair p = (Pair)_el.Tag;



                    foreach (Line line in _lines)
                    {
                        if (Math.Abs(line.X1 - _points[p.r, p.c].X) < 0.01 && Math.Abs(line.Y1 - _points[p.r, p.c].Y) < 0.01)
                        {
                            line.X1 = current.X + 3;
                            line.Y1 = current.Y + 3;
                        }
                        else if (Math.Abs(line.X2 - _points[p.r, p.c].X) < 0.01 && Math.Abs(line.Y2 - _points[p.r, p.c].Y) < 0.01)
                        {
                            line.X2 = current.X + 3;
                            line.Y2 = current.Y + 3;
                        }
                    }

                    _points[p.r, p.c].X = current.X + 3;
                    _points[p.r, p.c].Y = current.Y + 3;
                    //_el.Tag = p;

                    Canvas.SetLeft(_el, current.X);
                    Canvas.SetTop(_el, current.Y);
                }
            }
            catch (Exception ex)
            {
               // MessageBox.Show(ex.Message);
            }
        }

        private void el_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _el.ReleaseMouseCapture();
                _el = null;
            }
            catch (Exception ex)
            {
               // MessageBox.Show(ex.Message);
            }
        }
    }
}
