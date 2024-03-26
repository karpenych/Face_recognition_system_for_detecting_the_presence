using System.ComponentModel.DataAnnotations;

namespace face_rec_test1.Models
{
    public class AdminPanelModel
    {
        public class LoadVectorsModel
        {
            [Required(ErrorMessage = "Введите путь до папки")]
            public string? FolderPath { get; set; }
        }

        public class EmployeeModel
        {
            [Required(ErrorMessage = "Введите логин")]
            public string? Login { get; set; }

            [Required(ErrorMessage = "Введите пароль")]
            public string? Password { get; set; }

            public string? Full_name { get; set; }
        }

        public class FaceEncodingsModel
        {
            [Required(ErrorMessage = "Введите ID студента")]
            public int Student_id { get; set; }

            public double[]? Encoding { get; set; }
        }
    }
}