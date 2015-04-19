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
using System.Windows.Media.Imaging;

namespace gifenc
{
    public class SampleEffect : BaseEffect
    {
        private int _angle = 0;
        public override WriteableBitmap ProcessFrame(WriteableBitmap bmp)
        {
            if (_angle != 0)
                bmp = bmp.RotateFree(_angle % 360);
           _angle += 10;
        
            return bmp;
        }
    }
}
