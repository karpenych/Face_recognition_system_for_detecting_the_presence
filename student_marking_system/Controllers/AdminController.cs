﻿using Dapper;
using Emgu.CV.Dnn;
using Emgu.CV.Features2D;
using face_rec_test1.Models;
using FaceRecognitionDotNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using System.Text;
using System.Threading.RateLimiting;
using System.Windows.Markup;

namespace face_rec_test1.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Adminka()
        {
            if (UserInfo.UserLogin != "admin_msun")
                return RedirectToAction("Login", "Account");

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> LoadVectors(Models.AdminPanelModel.LoadVectorsModel model)
        {
            var directory = new DirectoryInfo(model.FolderPath);

            if (directory.Exists)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                FaceRecognition.InternalEncoding = System.Text.Encoding.GetEncoding("windows-1251");

                using FaceRecognition fr = FaceRecognition.Create($"{Environment.CurrentDirectory}\\Face_Rec_Models");

                StringBuilder query_sb = new("INSERT INTO face_encodings VALUES ");

                DirectoryInfo[] group_dirs = directory.GetDirectories();

                foreach (DirectoryInfo group_dir in group_dirs)
                {
                    Console.WriteLine("------Папка группы------");
                    Console.WriteLine(group_dir.Name);

                    DirectoryInfo[] student_id_dirs = group_dir.GetDirectories();

                    foreach (DirectoryInfo student_id_dir in student_id_dirs)
                    {

                        Console.WriteLine("*****Папка студента*****");
                        Console.WriteLine(student_id_dir.Name);

                        FileInfo[] photos = student_id_dir.GetFiles();
                        Console.WriteLine("____Фотки____");

                        foreach (FileInfo photo_file in photos)
                        {
                            Console.WriteLine(photo_file.Name);

                            using Image photo = FaceRecognition.LoadImageFile(photo_file.FullName);
                            List<Location> faces = fr.FaceLocations(photo).ToList();

                            if (faces.Count > 0)
                            {
                                Location face = faces[0];
                                int main_face_square = (face.Bottom - face.Top) * (face.Right - face.Left);

                                for (int i = 1; i < faces.Count; ++i)
                                {
                                    Location cur_face = faces[i];
                                    int cur_face_square = (cur_face.Bottom - cur_face.Top) * (cur_face.Right - cur_face.Left);
                                    if (cur_face_square > main_face_square)
                                    {
                                        face = cur_face;
                                        main_face_square = cur_face_square;
                                    }
                                }

                                Console.WriteLine($"t: {face.Top}, l: {face.Left}, b:{face.Bottom}, r:{face.Right}");

                                IEnumerable<FaceEncoding> face_encoding = fr.FaceEncodings(photo, new List<Location>() { face });
                                double[] face_vector = face_encoding.First().GetRawEncoding();

                                Console.WriteLine($"rank {face_vector.Rank}, length {face_vector.Length}");

                                var face_vector_sb = new StringBuilder();

                                for (int i = 0; i < face_vector.Length - 1; ++i) 
                                    face_vector_sb.Append($"{face_vector[i]},");
                                face_vector_sb.Append(face_vector[face_vector.Length - 1]);

                                query_sb.Append($"({student_id_dir.Name}, '{{{face_vector_sb}}}'), ");
                            }
                        }
                    }
                }

                Console.WriteLine();
                Console.WriteLine();

                char[] charsToTrim = { ' ', ',' };
                string query = query_sb.ToString().TrimEnd(charsToTrim);

                try
                {
                    await UserInfo.Connection.ExecuteAsync(query);
                }
                catch 
                {
                    Console.WriteLine("Ошибка вставки векторов лиц");
                    return RedirectToAction("Adminka", "Admin");
                }

                Console.WriteLine("Вставка окончена");
            }
            else
            {
                Console.WriteLine("Такого пути не существует");
            }  

            return RedirectToAction("Adminka", "Admin");
        }


        [HttpPost]
        public async Task<IActionResult> AddEmployee(Models.AdminPanelModel.AddEmployeeModel model)
        {
            Console.Write("Вставка: ");
            Console.WriteLine($"Логин: {model.Login}, Пароль: {model.Password}, ФИО: {model.Full_name}");

            try
            {
                await UserInfo.Connection.ExecuteAsync(
                    "INSERT INTO employees (login, password, full_name) VALUES (@Login, @Password, @Full_name)", model);
            }
            catch
            {
                Console.WriteLine("Ошибка вставки работника");
            }
            Console.WriteLine("Вставка работника прошла успешно");

            return RedirectToAction("Adminka", "Admin");
        }


        [HttpPost]
        public async Task<IActionResult> AddTeacherSubject(Models.AdminPanelModel.AddTeacherSubjectModel model)
        {
            Console.Write("Вставка: ");
            Console.WriteLine($"Логин: {model.Login}, Группа: {model.Group_id}, Предмет: {model.Subject}");

            try
            {
                await UserInfo.Connection.ExecuteAsync(
                    "INSERT INTO teachers_groups (login, group_id, subject) VALUES (@Login, @Group_id, @Subject)", model);
            }
            catch
            {
                Console.WriteLine("Ошибка вставки преподаватель-группа-предмет");
            }

            Console.WriteLine("Вставка преподаватель-группа-предмет прошла успешно");

            return RedirectToAction("Adminka", "Admin");
        }


        [HttpPost]
        public async Task<IActionResult> AddStudent(Models.AdminPanelModel.AddStudentModel model)
        {
            Console.Write("Вставка: ");
            Console.WriteLine($"Группа: {model.Group_id}, ФИО: {model.Full_name}");

            try
            {
                await UserInfo.Connection.ExecuteAsync(
                    "INSERT INTO students (group_id, full_name) VALUES (@Group_id, @Full_name)", model);
            }
            catch
            {
                Console.WriteLine("Ошибка вставки студента");
            }

            Console.WriteLine("Вставка студента прошла успешно");

            return RedirectToAction("Adminka", "Admin");
        }


    }
}
