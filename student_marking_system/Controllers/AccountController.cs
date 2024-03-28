using Dapper;
using Npgsql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace face_rec_test1.Controllers
{
    public class AccountController : Controller
    {
        static string host = "localhost";
        static string port = "5432";
        static string admin = "admin_msun";
        static string adm_password = "qwer1";
        static string database = "db_face_recognition";


        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Login(Models.LoginModel? model)
        {
            if (UserInfo.Connection == null)
            {
                UserInfo.Connection = new NpgsqlConnection(
                connectionString: $"Server={host};Port={port};User Id={admin};Password={adm_password};Database={database}");

                try
                {
                    UserInfo.Connection.Open();
                }
                catch
                {
                    ModelState.AddModelError("", "Сервер не доступен");
                    return View(model);
                }
            }

            IEnumerable<Models.TeacherPanelModel.TeacherEnterModel> user;

            try
            {
                user = UserInfo.Connection.Query<Models.TeacherPanelModel.TeacherEnterModel>(
                    "SELECT login, full_name FROM employees WHERE login=@Login AND password=@Password", new { model.Login, model.Password });
            }
            catch
            {
                ModelState.AddModelError("", "Ошибка во время выполнения запроса");
                return View(model);
            }

            if (user.Any())
            {
                UserInfo.UserLogin = user.First().Login;
                UserInfo.UserFullName = user.First().Full_name;

                Console.WriteLine($"Здравствуйте {UserInfo.UserFullName} ({UserInfo.UserLogin})\n");

                if (UserInfo.UserLogin == "admin_msun")
                {
                    return RedirectToAction("Adminka", "Admin");
                }
                else
                {
                    try
                    {
                        UserInfo.Subjects = UserInfo.Connection.Query<string>(
                            "SELECT DISTINCT subject FROM teachers_groups WHERE login=@UserLogin", new { UserInfo.UserLogin }).ToArray();
                    }
                    catch
                    {
                        ModelState.AddModelError("", "Не удалось получить список ваших предметов с сервера БД");
                        return View(model);
                    }

                    Console.WriteLine("Вы преподаете предметы:");
                    foreach (var subject in UserInfo.Subjects)
                        Console.WriteLine(subject);
                    Console.WriteLine();

                    return RedirectToAction("Index", "Home");
                }
            }
            
            ModelState.AddModelError("", "Некорректный логин (и/или) пароль");
            return View(model);
        }


        public IActionResult LogOut()
        {
            Console.WriteLine($"Досвидания {UserInfo.UserFullName}.\n");

            UserInfo.Subjects = Array.Empty<string>();

            UserInfo.UserLogin = null;
            UserInfo.UserFullName = null;

            return RedirectToAction("Login", "Account");
        }

    }
}
