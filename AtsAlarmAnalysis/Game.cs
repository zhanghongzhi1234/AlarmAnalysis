using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateProject
{
    /// <summary>
    /// Helper class to simulate a game
    /// </summary>
    public class Game : INotifyPropertyChanged
    {
        private double score;

        public double Score
        {
            get { return score; }
            set
            {
                score = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Score"));
                }
            }
        }


        public Game(double scr)
        {
            this.Score = scr;
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
