using GanttProgram.Helper;
using System.Windows;
using System.Windows.Input;

namespace GanttProgram
{
    public abstract class Dialog : Window
    {
        public ICommand? SaveCommand { get; protected set; }
        public ICommand? CloseCommand { get; protected set; }

        protected Dialog()
        {
            SaveCommand = new RelayCommand(_ => SaveDialog(null, null));
            CloseCommand = new RelayCommand(_ => this.Close());

            this.Closing += OnDialogClosing;
        }

        protected virtual void SaveDialog(object? sender, RoutedEventArgs? e)
        {
        }
        
        protected virtual void CloseDialog(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected virtual void OnDialogClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        public new bool? ShowDialog()
        {
            return base.ShowDialog();
        }
    }
}
