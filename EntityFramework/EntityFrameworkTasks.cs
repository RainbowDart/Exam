using DatabaseCourseExam.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace DatabaseCourseExam.EntityFramework;

public static class EntityFrameworkTasks
{
    // задание 1, 6, 7
    public static async Task ShowStudentsAsync()
    {
        await using var context = SchoolDbContextFactory.Create();

        // фильтр и сортировка
        List<Student> students = await context.Students
            .Include(student => student.Group)
            .Where(student => student.Age >= 18) // фильтр
            .OrderBy(student => student.LastName) //  сортировка
            .ThenBy(student => student.FirstName)
            .ToListAsync();

        Console.WriteLine("Студенты (EF Core)");

        foreach (Student student in students)
        {
            Console.WriteLine(
                $"{student.Id}. {student.FirstName} {student.LastName}, " +
                $"возраст: {student.Age}, группа: {student.Group.Name}");
        }

        //  группировка
        Console.WriteLine("\nКоличество студентов по группам");

        var groups = students
            .GroupBy(student => student.Group.Name)
            .Select(group => new
            {
                GroupName = group.Key,
                Count = group.Count()
            });

        foreach (var group in groups)
        {
            Console.WriteLine($"{group.GroupName}: {group.Count}");
        }
    }

    // задание 8
    public static async Task UpdateStudentAgeAsync(int studentId, int newAge)
    {
        await using var context = SchoolDbContextFactory.Create();
        Student? student = await context.Students
            .FirstOrDefaultAsync(x => x.Id == studentId);

        if (student is null)
        {
            Console.WriteLine("Студент не найден.");
            return;
        }

        student.Age = newAge;

        try
        {
            int affectedRows = await context.SaveChangesAsync();
            Console.WriteLine($"Возраст изменён. Сохранено изменений: {affectedRows}");
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"Ошибка сохранения: {ex.InnerException?.Message ?? ex.Message}");        }
    }

    public static async Task ShowCoursesAsync()
    {
        await using var context = SchoolDbContextFactory.Create();

        var courses = await context.Courses
            .OrderBy(course => course.Name)
            .Select(course => new
            {
                course.Name,
                course.Price,
                course.AvailablePlaces
            })
            .ToListAsync();

        foreach (var course in courses)
        {
            Console.WriteLine(
                $"{course.Name} | Цена: {course.Price:N2} | " +
                $"Свободных мест: {course.AvailablePlaces}");
        }
    }
}