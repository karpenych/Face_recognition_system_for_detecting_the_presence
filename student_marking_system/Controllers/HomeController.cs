using Amazon.S3;
using Amazon.S3.Model;
using Dapper;
using Emgu.CV;
using Emgu.CV.CvEnum;
using face_rec_test1.Models;
using FaceRecognitionDotNet;
using Microsoft.AspNetCore.Mvc;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;



namespace face_rec_test1.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (UserInfo.UserLogin == "admin_msun")
                return RedirectToAction("Login", "Account");

            return View();
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


        public JsonResult GetCameras()
        {
            List<int> cameras = new();
            int cur_camera_id = 0;

            while (true)
            {
                var camera = new VideoCapture(cur_camera_id);

                if (!camera.IsOpened)
                {
                    break;
                }

                cameras.Add(cur_camera_id);
                cur_camera_id++;
            }

            return Json(cameras); 
        }


        public JsonResult GetSerialPorts()
        {
            return Json(SerialPort.GetPortNames());
        }


        struct CameraHandleParams
        {
            public int Camera_id { get; set; }
            public string? Port_name { get; set; }
        }


        public IActionResult LessonStart(Models.TeacherPanelModel.TeacherGetStudentsModel model)
        {
            try
            {
                UserInfo.CurStudentsNames = UserInfo.Connection.Query<TeacherPanelModel.TeacherStudentsName>(
                    "SELECT id, full_name FROM students WHERE group_id=@Group_id", new { model.Group_id }).OrderBy(x => x.Full_name).ThenBy(x => x.Id).ToArray();
            }
            catch
            {
                Console.WriteLine("Ошибка взятия информации о студентах");
                return RedirectToAction("Login", "Account");
            }

            try
            {
                UserInfo.CurStudentsEncoodings = UserInfo.Connection.Query<TeacherPanelModel.TeacherStudentsEncoding>(
                    "SELECT student_id, encoding FROM face_encodings WHERE group_id=@Group_id", new { model.Group_id }).ToArray();
            }
            catch
            {
                Console.WriteLine("Ошибка взятия информации о векторах");
                return RedirectToAction("Login", "Account");
            }

            Console.WriteLine($"Информация о векторах {model.Group_id}:");
            foreach (var s in UserInfo.CurStudentsEncoodings)
                Console.WriteLine($"{s.Student_id} - {s.Encoding}");
            Console.WriteLine();

            UserInfo.CurSubject = model.Subject;
            UserInfo.CurGroup = model.Group_id;

            UserInfo.IsLesson = true;
            UserInfo.IsCameraWorking = true;

            CameraHandleParams thread_parametrs = new() 
            { 
                Camera_id = model.Camera_id, 
                Port_name = model.Port_name 
            };

            var start_camera = new Thread(new ParameterizedThreadStart(CameraHandleLoop));
            start_camera.Start(thread_parametrs);

            Console.WriteLine("Занятие началось!");

            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        public void LessonOff()
        {
            WriteInSessionLog(UserInfo.CurGroup, UserInfo.CurSubject, UserInfo.CurStudentsNames);

            UserInfo.CurSubject = null;
            UserInfo.CurGroup = null;
            
            UserInfo.IsCameraWorking = false;
            UserInfo.IsLesson = false;

            UserInfo.CurStudentsNames = Array.Empty<TeacherPanelModel.TeacherStudentsName>();
            UserInfo.CurStudentsEncoodings = Array.Empty<TeacherPanelModel.TeacherStudentsEncoding>();

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


        private void CameraHandleLoop(object parametrs_)
        {
            CameraHandleParams parametrs = (CameraHandleParams)parametrs_;

            SerialPort serial_port = new(parametrs.Port_name, 250000, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.None
            };
            serial_port.Open();

            const double tolerance = 0.6d;


            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            FaceRecognition.InternalEncoding = System.Text.Encoding.GetEncoding("windows-1251");

            using FaceRecognition fr = FaceRecognition.Create($"{Environment.CurrentDirectory}\\Face_Rec_Models");

            while (UserInfo.IsLesson)
            { 
                if (UserInfo.IsCameraWorking)
                {
                    var camera = new VideoCapture(parametrs.Camera_id);
                    camera.Set(CapProp.Fps, 30);
                    camera.Set(CapProp.FrameHeight, 450);
                    camera.Set(CapProp.FrameWidth, 370);

                    if (camera.IsOpened)  // Обработка кадров 
                    {
                        Console.WriteLine("Камера подключена");
                        serial_port.Write("t");

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
                                        if (!counts.ContainsKey(student.Student_id))
                                            counts.Add(student.Student_id, 0);

                                        counts[student.Student_id]++;
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

                                            serial_port.Write("y");

                                            Thread.Sleep(2000);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Обнаружен шпион...");

                                    serial_port.Write("n");

                                    Thread.Sleep(1000);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Никого нет");
                                Thread.Sleep(2000);
                            }
                        }
                        serial_port.Write("o");
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
            serial_port.Close();
        }


        static private async void WriteInSessionLog(string group_id, string subject, Models.TeacherPanelModel.TeacherStudentsName[] cur_students)
        { 
            var configsS3 = new AmazonS3Config
            {
                ServiceURL = "https://s3.yandexcloud.net"
            };
            var s3client = new AmazonS3Client(configsS3);

            var getRequest = new GetObjectRequest
            {
                BucketName = group_id,
                Key = $"{group_id}_{subject}.csv"
            };

            using var getResponse = await s3client.GetObjectAsync(getRequest);
            using var sr = new StreamReader(getResponse.ResponseStream);      
            var responseString = await sr.ReadToEndAsync();
            var lines = responseString.Split("\r\n").ToList();

            lines[0] = $"{lines[0]},{DateTime.Now:dd-MM-yyyy}";

            for (int i = 1; i < lines.Count; ++i)
            {
                lines[i] += cur_students[i - 1].IsThere ? ",+" : ",-";
            }

            var textToSend = string.Join("\r\n", lines);
            Console.WriteLine(textToSend);
            
            var textAsBytes = Encoding.UTF8.GetBytes(textToSend);

            using var ms = new MemoryStream(textAsBytes);
            ms.Position = 0;

            var putRequest = new PutObjectRequest
            {
                BucketName = group_id,
                Key = $"{group_id}_{subject}.csv",
                InputStream = ms,
            };

            await s3client.PutObjectAsync(putRequest);
        }


    }
}
