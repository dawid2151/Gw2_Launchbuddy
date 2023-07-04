using Gw2_Launchbuddy.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gw2_Launchbuddy.Extensions.Triggers
{
    public class OpenCVElementExistsTrigger : IProcessTrigger
    {
        GwGameProcess process;
        Bitmap template;

        public Process found_process;
        public OpenCVElementExistsTrigger(GwGameProcess process, Bitmap template)
        {
            this.process = process;
            this.template = template;
        }
        public bool IsActive
        {
            get
            {
                try
                {
                    var screenshot = WindowUtil.PrintWindow(process.MainWindowHandle);
                    bool elementExists = ScreenAnalyser.DoesTemplateExists(screenshot, template);
                    return elementExists;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }
    }
}
