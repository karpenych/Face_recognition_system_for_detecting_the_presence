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

        public class AddEmployeeModel
        {
            [Required(ErrorMessage = "Введите логин")]
            public string? Login { get; set; }

            [Required(ErrorMessage = "Введите пароль")]
            public string? Password { get; set; }

            public string? Full_name { get; set; }
        }

        public class AddTeacherSubjectModel
        {
            [Required(ErrorMessage = "Введите логин")]
            public string? Login { get; set; }

            [Required(ErrorMessage = "Введите группу")]
            public string? Group_id { get; set; }

            [Required(ErrorMessage = "Введите предмет")]
            public string? Subject { get; set; }
        }

        public class AddStudentModel
        {
            [Required(ErrorMessage = "Введите ID")]
            public int? Id { get; set; }

            [Required(ErrorMessage = "Введите группу")]
            public string? Group_id { get; set; }

            [Required(ErrorMessage = "Введите ФИО")]
            public string? Full_name { get; set; }
        }

    }
}