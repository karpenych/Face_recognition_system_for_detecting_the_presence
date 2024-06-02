using System.ComponentModel.DataAnnotations;


namespace face_rec_test1.Models
{
    public class TeacherPanelModel
    {
        public class TeacherEnterModel
        {
            public string? Login { get; set; }

            public string? Full_name { get; set; }
        }

        public class TeacherGetStudentsModel
        {
            [Required(ErrorMessage="Выберите предмет")]
            public string? Subject { get; set; }

            [Required(ErrorMessage = "Выберите группу")]
            public string? Group_id { get; set; }

            [Required(ErrorMessage = "Выберите камеру")]
            public int Camera_id { get; set; }
        }

        public class TeacherStudentsName
        {
            public int Id { get; set; }

            public string? Full_name { get; set; }

            public bool IsThere { get; set; }
        }

        public class TeacherStudentsEncoding
        {
            public int Student_id { get; set; }

            public double[]? Encoding { get; set; }
        }
    }
}
