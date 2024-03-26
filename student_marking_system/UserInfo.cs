using face_rec_test1.Models;
using Npgsql;

namespace face_rec_test1
{
    public static class UserInfo
    {
        public static NpgsqlConnection Connection { get; set; }

        public static string? UserLogin { get; set; }

        public static string? UserFullName { get; set; }

        public static Models.TeacherPanelModel.TeacherStudentsName[] CurStudentsNames { get; set; }

        public static Models.TeacherPanelModel.TeacherStudentsEncoding[] CurStudentsEncoodings { get; set; }

        public static bool IsCameraWorking {  get; set; }

        public static bool IsLesson { get; set; }


    }
}
