using DatabaseCourseExam.Data;
using Dapper;
using DatabaseCourseExam.DapperTasks.DTO;

namespace DatabaseCourseExam.DapperTasks;

public static class DapperStudentTasks
{
    // задание 2
    public static async Task ShowStudentsAsync(string? groupName = null)
    {
        await using var connection = DatabaseConnection.Create();

        const string sql = """
            SELECT
                s.id AS Id,
                s.first_name AS FirstName,
                s.last_name AS LastName,
                s.age AS Age,
                g.name AS GroupName
            FROM students AS s
            JOIN student_groups AS g ON g.id = s.group_id
            WHERE (@GroupName IS NULL OR g.name = @GroupName)
            ORDER BY s.id;
            """;

        var students = await connection.QueryAsync<StudentDto>(sql, new { GroupName = groupName });

        foreach (StudentDto student in students)
        {
            Console.WriteLine(
                $"{student.Id}. {student.FirstName} {student.LastName}, " +
                $"возраст: {student.Age}, группа: {student.GroupName}");
        }
    }

    // задание 13
    public static async Task UpdateStudentAgeAsync(int studentId, int newAge)
    {
        if (newAge is < 14 or > 100)
        {
            Console.WriteLine("Возраст должен быть от 14 до 100.");
            return;
        }

        await using var connection = DatabaseConnection.Create();

        const string sql = """
            UPDATE students SET age = @NewAge WHERE id = @StudentId;
            """;

        int affectedRows = await connection.ExecuteAsync(
            sql, new { NewAge = newAge, StudentId = studentId });

        Console.WriteLine(affectedRows == 0
            ? "Студент не найден."
            : $"Изменено записей: {affectedRows}");
    }

    // задание 9
    public static async Task ShowCoursesAsync()
    {
        await using var connection = DatabaseConnection.Create();

        const string sql = """
            SELECT
                c.name AS Name,
                c.price AS Price,
                c.available_places AS AvailablePlaces,
                COUNT(sc.student_id) AS StudentCount
            FROM courses AS c
            LEFT JOIN student_courses AS sc ON sc.course_id = c.id
            GROUP BY c.id, c.name, c.price, c.available_places
            ORDER BY c.name;
            """;

        var courses = await connection.QueryAsync<dynamic>(sql);

        foreach (var course in courses)
        {
            Console.WriteLine(
                $"{course.name} | Цена: {course.price:N2} | " +
                $"Мест: {course.availableplaces} | Студентов: {course.studentcount}");
        }
    }

    // задание 12
    public static async Task EnrollStudentAsync(int studentId, int courseId)
    {
        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // проверка студента
            bool studentExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM students WHERE id = @StudentId)",
                new { StudentId = studentId }, transaction);

            if (!studentExists)
                throw new InvalidOperationException("Студент не найден.");

            // на повторную запись
            bool alreadyEnrolled = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM student_courses WHERE student_id = @StudentId AND course_id = @CourseId)",
                new { StudentId = studentId, CourseId = courseId }, transaction);

            if (alreadyEnrolled)
                throw new InvalidOperationException("Студент уже записан на этот курс.");

            // уменьшаем места
            const string updateSql = """
                UPDATE courses
                SET available_places = available_places - 1
                WHERE id = @CourseId AND available_places > 0
                RETURNING available_places;
                """;

            int? remainingPlaces = await connection.QuerySingleOrDefaultAsync<int?>(
                updateSql, new { CourseId = courseId }, transaction);

            if (remainingPlaces is null)
                throw new InvalidOperationException("Курс не найден или мест нет.");

            // вставляем запись
            await connection.ExecuteAsync(
                "INSERT INTO student_courses (student_id, course_id) VALUES (@StudentId, @CourseId)",
                new { StudentId = studentId, CourseId = courseId }, transaction);

            await transaction.CommitAsync();
            Console.WriteLine($"Записан. Осталось мест: {remainingPlaces}");
        }
        //отменяем
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Операция отменена: {ex.Message}");
        }
    }
}