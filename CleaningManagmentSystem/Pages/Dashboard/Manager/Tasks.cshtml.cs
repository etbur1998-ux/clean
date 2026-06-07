using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Manager
{
    public class TasksModel : PageModel
    {
        private readonly string _connectionString;

        public TasksModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public List<TaskItem> TasksList { get; set; } = new();

        [BindProperty] public string TaskNumber { get; set; } = "";
        [BindProperty] public string Description { get; set; } = "";
        [BindProperty] public string Priority { get; set; } = "Normal";
        [BindProperty] public DateTime TaskDate { get; set; } = DateTime.Today;

        public void OnGet()
        {
            LoadTasks();
        }

        public IActionResult OnPostCreate()
        {
            using var connection = new MySqlConnection(_connectionString);
            string q = "INSERT INTO delivery_tasks (task_number, description, priority, task_date, status, created_at) VALUES (@TaskNumber, @Description, @Priority, @TaskDate, 'Pending', NOW())";
            try {
                connection.Execute(q, new { TaskNumber, Description, Priority, TaskDate });
            } catch {
                // Ignore unique constraint errors for now or handle accordingly
            }
            return RedirectToPage();
        }

        private void LoadTasks()
        {
            using var connection = new MySqlConnection(_connectionString);
            TasksList = connection.Query<TaskItem>(
                "SELECT id, task_number, description, priority, task_date, status FROM delivery_tasks ORDER BY id DESC"
            ).ToList();
        }

        public class TaskItem
        {
            public int id { get; set; }
            public string task_number { get; set; } = "";
            public string description { get; set; } = "";
            public string priority { get; set; } = "";
            public DateTime task_date { get; set; }
            public string status { get; set; } = "";
        }
    }
}
