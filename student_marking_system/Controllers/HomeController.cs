using face_rec_test1.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Runtime.CompilerServices;
using FaceRecognitionDotNet;
using Dapper;
using Emgu.CV.Dnn;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Razor.Infrastructure;

namespace face_rec_test1.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (UserInfo.UserLogin == "admin_msun")
                return RedirectToAction("Login", "Account");

            ViewBag.Subjects = GetSubjects();

            return View();
        }


        public IEnumerable<string> GetSubjects()
        {
            try
            {
                var subjects = UserInfo.Connection.Query<string>(
                    "SELECT DISTINCT subject FROM teachers_groups WHERE login=@UserLogin", new { UserInfo.UserLogin }).ToList();

                Console.WriteLine("Вы преподаете предметы:");
                foreach (var subject in subjects)
                    Console.WriteLine(subject);
                Console.WriteLine();


                return subjects;
            }
            catch
            {
                Console.WriteLine("Ошибка взятия Subjects");
                return null;
            }
        }


        public JsonResult GetGroups(string subject)
        {
            try
            {
                var groups = UserInfo.Connection.Query<string>(
                    "SELECT group_id FROM teachers_groups WHERE login=@UserLogin AND subject=@Subject", new { UserInfo.UserLogin, subject });

                Console.WriteLine($"Предмет {subject} Вы преподаете в группах:");
                foreach (var group in groups)
                    Console.WriteLine(group);
                Console.WriteLine();

                return Json(groups);
            }
            catch
            {
                Console.WriteLine("Ошибка взятия Groups");
                return null;
            }
        }

        public IActionResult LessonStart(Models.TeacherPanelModel.TeacherGetStudentsModel model)
        {
            try
            {
                UserInfo.CurStudentsNames = UserInfo.Connection.Query<TeacherPanelModel.TeacherStudentsName>(
                    "SELECT id, full_name FROM students WHERE group_id=@Group_id", new { model.Group_id }).ToList();

                UserInfo.CurStudentsEncoodings = UserInfo.Connection.Query<TeacherPanelModel.TeacherStudentsEncoding>(
                    "SELECT s.id, fe.encoding FROM students s JOIN face_encodings fe ON s.id=fe.student_id WHERE s.group_id=@Group_id", new { model.Group_id }).ToList();
            }
            catch
            {
                Console.WriteLine("Ошибка взятия информации о учителе");
                return RedirectToAction("Login", "Account");
            }

            Console.WriteLine($"Информация о векторах {model.Group_id}:");
            foreach (var s in UserInfo.CurStudentsEncoodings)
                Console.WriteLine($"{s.Id} - {s.Encoding}");
            Console.WriteLine();


            UserInfo.IsLesson = true;
            UserInfo.IsCameraWorking = true;

            var start_camera = new Thread(CameraHandleLoop);
            start_camera.Start();

            Console.WriteLine("Занятие началось!");

            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        public void LessonOff()
        {
            UserInfo.IsCameraWorking = false;
            UserInfo.IsLesson = false;
            Console.WriteLine("Урок окончен");
        }


        [HttpPost]
        public void CameraOff()
        {
            UserInfo.IsCameraWorking = false;
            Console.WriteLine("Кавера выключена кнопкой");
        }


        [HttpPost] 
        public void CameraOn()
        {
            UserInfo.IsCameraWorking = true;
            Console.WriteLine("Кавера включена кнопкой");
        }  



        private void CameraHandleLoop()
        {
            while (UserInfo.IsLesson)
            { 
                if (UserInfo.IsCameraWorking)
                {
                    var camera = new VideoCapture(0);
                    camera.Set(CapProp.Fps, 30);
                    camera.Set(CapProp.FrameHeight, 450);
                    camera.Set(CapProp.FrameWidth, 370);

                    if (camera.IsOpened)  // Обработка кадров 
                    {
                        Console.WriteLine("Камера подключена");

                        while (UserInfo.IsCameraWorking)
                        {
                            var frame = camera.QueryFrame();

                            if (frame == null)
                            {
                                Console.WriteLine("Не удалось захватить кадр");
                                break;
                            }

                            Console.WriteLine("Life is Good");
                            Thread.Sleep(2000);
                        }

                        camera.Dispose();
                        Thread.Sleep(1000);
                    }
                    else  // Пробуем переподключится
                    {
                        Console.WriteLine("Камера не подключена");
                        camera.Dispose();
                        Thread.Sleep(1000);
                    }
                }
            }
        }

        
    }
}
