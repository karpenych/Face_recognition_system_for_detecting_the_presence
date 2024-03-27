using System.ComponentModel.DataAnnotations;

namespace face_rec_test1.Models
{

    public class LoginModel
    {
        [Required(ErrorMessage = "Не указан логин")]
        public string? Login { get; set; }
        
        [Required(ErrorMessage = "Не указан пароль")]
        [DataType(DataType.Password)] 
        public string? Password { get; set; }
    }
}
