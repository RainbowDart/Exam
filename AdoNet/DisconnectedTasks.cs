using System.Data;
using DatabaseCourseExam.Data;
using Npgsql;

namespace DatabaseCourseExam.AdoNet;

public static class DisconnectedTasks
{
    // задание 10
    public static async Task<DataTable> LoadStudentsAsync()
    {
        var dataSet = new DataSet("School");

        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();

        // Students
        var studentsCmd = new NpgsqlCommand(
            "SELECT id, first_name, last_name, age, group_id FROM students", connection);
        var studentsTable = new DataTable("Students");
        new NpgsqlDataAdapter(studentsCmd).Fill(studentsTable);
        studentsTable.PrimaryKey = [studentsTable.Columns["id"]!];

        // Groups
        var groupsCmd = new NpgsqlCommand(
            "SELECT id, name FROM student_groups", connection);
        var groupsTable = new DataTable("Groups");
        new NpgsqlDataAdapter(groupsCmd).Fill(groupsTable);
        groupsTable.PrimaryKey = [groupsTable.Columns["id"]!];

        dataSet.Tables.Add(groupsTable);
        dataSet.Tables.Add(studentsTable);

        // DataRelation
        dataSet.Relations.Add(
            "GroupStudents",
            groupsTable.Columns["id"]!,
            studentsTable.Columns["group_id"]!);

        return studentsTable;
    }

    public static void PrintTable(DataTable table)
    {
        foreach (DataRow row in table.Rows)
        {
            Console.WriteLine(
                $"{row["id"]}: {row["first_name"]} " +
                $"{row["last_name"]}, возраст {row["age"]}");
        }
    }

    // задание 4
    public static void ChangeAgeInMemory(DataTable table, int studentId, int newAge)
    {
        DataRow? row = table.Rows.Find(studentId);

        if (row is null)
        {
            Console.WriteLine("Студент не найден в DataTable.");
            return;
        }

        if ((int)row["age"] == newAge)
        {
            Console.WriteLine("Возраст не изменился.");
            return;
        }

        row["age"] = newAge;
        Console.WriteLine($"Возраст изменён в памяти. Состояние строки: {row.RowState}");
    }

    // задание 11
    public static int SaveChanges(DataTable table)
    {
        using var connection = DatabaseConnection.Create();
        connection.Open();

        const string sql = """
            UPDATE students SET age = @age WHERE id = @id;
            """;

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add("@age", NpgsqlTypes.NpgsqlDbType.Integer, 4, "age");
        command.Parameters.Add("@id", NpgsqlTypes.NpgsqlDbType.Integer, 4, "id");

        using var adapter = new NpgsqlDataAdapter();
        adapter.UpdateCommand = command;

        int affectedRows = adapter.Update(table);
        Console.WriteLine($"Сохранено изменений: {affectedRows}");

        foreach (DataRow row in table.Rows)
        {
            if (row.RowState == DataRowState.Modified)
            {
                Console.WriteLine($"Строка {row["id"]} всё ещё в состоянии Modified");
            }
        }

        return affectedRows;
    }
}