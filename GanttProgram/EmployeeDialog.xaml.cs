using GanttProgram.Helper;
using GanttProgram.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Windows;

namespace GanttProgram
{
    public partial class EmployeeDialog : Dialog
    {
        private Employee _employee;
        private readonly bool _isEditMode;

        public EmployeeDialog(Employee selectedEmployee)
        {
            InitializeComponent();
            _isEditMode = true;
            SaveCommand = new RelayCommand(_ => SaveDialog(null, null));
            CloseCommand = new RelayCommand(_ => this.Close());
            Title = $"Mitarbeiter \"{selectedEmployee}\" bearbeiten";
            EditDialog_Loaded(selectedEmployee);
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

        protected void EditDialog_Loaded(Employee selectedEmployee)
        {
            Loaded += async (s, e) =>
            {
                await using var context = new GanttDbContext();
                var employeeEntity = await context.Employee
                    .FirstOrDefaultAsync(emp => emp.Id == selectedEmployee.Id);

                if (employeeEntity != null)
                {
                    _employee = employeeEntity;
                    NameTextBox.Text = _employee.LastName;
                    VornameTextBox.Text = _employee.FirstName ?? string.Empty;
                    AbteilungTextBox.Text = _employee.Department ?? string.Empty;
                    TelefonTextBox.Text = _employee.Phone ?? string.Empty;
                }
                else
                {
                    MessageBox.Show("Mitarbeiter nicht gefunden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            };
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
