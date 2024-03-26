﻿using face_rec_test1.Models;
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
using System.Text;
using Emgu.CV.Structure;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

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
                    "SELECT id, full_name FROM students WHERE group_id=@Group_id", new { model.Group_id }).ToArray();

                UserInfo.CurStudentsEncoodings = UserInfo.Connection.Query<TeacherPanelModel.TeacherStudentsEncoding>(
                    "SELECT s.id, fe.encoding FROM students s JOIN face_encodings fe ON s.id=fe.student_id WHERE s.group_id=@Group_id", new { model.Group_id }).ToArray();
            }
            catch
            {
                Console.WriteLine("Ошибка взятия информации о учителе");
                return RedirectToAction("Login", "Account");
            }

            Console.WriteLine($"Информация о студентах {model.Group_id}:");
            foreach (var s in UserInfo.CurStudentsNames)
                Console.WriteLine($"{s.Id} - {s.Full_name} - {s.IsThere}");
            Console.WriteLine();

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
            UserInfo.CurStudentsNames = null;
            UserInfo.CurStudentsEncoodings = null;
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
            const double tolerance = 0.6d;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            FaceRecognition.InternalEncoding = System.Text.Encoding.GetEncoding("windows-1251");

            using FaceRecognition fr = FaceRecognition.Create($"{Environment.CurrentDirectory}\\Face_Rec_Models");

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

                            var bytes = new byte[frame.Rows * frame.Cols * frame.ElementSize];
                            Marshal.Copy(frame.DataPointer, bytes, 0, bytes.Length);

                            using Image photo = FaceRecognition.LoadImage(bytes, frame.Rows, frame.Cols, frame.ElementSize * frame.Cols, Mode.Rgb);

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

                                FaceEncoding unknown_encoding = fr.FaceEncodings(photo, new List<Location>() { face }).First();

                                Dictionary<int, int> counts = new();

                                foreach (var student in UserInfo.CurStudentsEncoodings)
                                {
                                    FaceEncoding known_encoding = FaceRecognition.LoadFaceEncoding(student.Encoding);

                                    if (FaceRecognition.CompareFace(known_encoding, unknown_encoding, tolerance))
                                    {
                                        if (!counts.ContainsKey(student.Id))
                                            counts.Add(student.Id, 0);

                                        counts[student.Id]++;
                                    }
                                }

                                if (counts.Count > 0)
                                {
                                    var students_id_to_mark = counts.MaxBy(x => x.Value).Key;

                                    foreach (var student in UserInfo.CurStudentsNames)
                                    {
                                        if (student.Id == students_id_to_mark)
                                        {
                                            student.IsThere = true;
                                            Console.WriteLine($"Пришел {student.Full_name} ({student.Id})");
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Броу, это не твоя пара...");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Никого нет");
                            }

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
