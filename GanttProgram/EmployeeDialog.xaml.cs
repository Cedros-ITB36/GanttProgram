using System.Text.RegularExpressions;
using GanttProgram.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using GanttProgram.Helper;

namespace GanttProgram
{
    public partial class EmployeeDialog : Dialog
    {
        private readonly Employee _employee;
        private readonly bool _isEditMode;

        public EmployeeDialog(Employee selectedEmployee)
        {
            InitializeComponent();
            _employee = selectedEmployee;
            _isEditMode = true;
            Loaded += EditDialog_Loaded;
            SaveCommand = new RelayCommand(_ => SaveDialog(null, null));
            CloseCommand = new RelayCommand(_ => this.Close());
            Title = $"Mitarbeiter \"{_employee}\" bearbeiten";
        }
        public EmployeeDialog()
        {
            InitializeComponent();
            _employee = new Employee();
            _isEditMode = false;
            SaveCommand = new RelayCommand(_ => SaveDialog(null, null));
            CloseCommand = new RelayCommand(_ => this.Close());
            Title = "Neuen Mitarbeiter erstellen";
        }

        protected override void EditDialog_Loaded(object sender, RoutedEventArgs e)
        {
            NameTextBox.Text = _employee.LastName;
            VornameTextBox.Text = _employee.FirstName ?? string.Empty;
            AbteilungTextBox.Text = _employee.Department ?? string.Empty;
            TelefonTextBox.Text = _employee.Phone ?? string.Empty;
        }

        protected override async void SaveDialog(object? sender, RoutedEventArgs? e)
        {
            _employee.LastName = NameTextBox.Text.Trim();
            _employee.FirstName = VornameTextBox.Text.Trim();
            _employee.Department = AbteilungTextBox.Text.Trim();
            _employee.Phone = TelefonTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(_employee.LastName))
            {
                MessageBox.Show("Bitte geben Sie einen Namen ein.", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!string.IsNullOrWhiteSpace(_employee.Phone) && !PhoneNumberRegex().IsMatch(_employee.Phone))
            {
                MessageBox.Show("Bitte geben Sie eine gültige Telefonnummer ein.", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await using (var context = new GanttDbContext())
            {
                var exists = await context.Employee
                    .AnyAsync(m => m.LastName == _employee.LastName && m.Id != _employee.Id);

                if (exists)
                {
                    MessageBox.Show("Ein Mitarbeiter mit diesem Namen existiert bereits.", "Fehler",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_isEditMode)
                {
                    context.Employee.Attach(_employee);
                    context.Entry(_employee).State = EntityState.Modified;
                }
                else
                {
                    context.Employee.Add(_employee);
                }

                await context.SaveChangesAsync();
            }

            DialogResult = true;
            Close();
        }

        [GeneratedRegex(@"^\+?[0-9][0-9\s().-]{5,20}[0-9]$")]
        private static partial Regex PhoneNumberRegex();
    }
}
