using MySqlConnector;
using System;
using System.Threading.Tasks;

namespace RunDbMigration
{
    class Program
    {
        static async Task Main()
        {
            var connectionString = "Server=localhost;Database=yeka_cleaning;User=root;Password=;";
            
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            Console.WriteLine("Connected to database.");

            // Add email_notifications column
            var cmd1 = new MySqlCommand(
                "ALTER TABLE users ADD COLUMN IF NOT EXISTS email_notifications BOOLEAN DEFAULT true AFTER phone;", 
                connection);
            await cmd1.ExecuteNonQueryAsync();
            Console.WriteLine("Added/verified email_notifications column.");

            // Add sms_notifications column
            var cmd2 = new MySqlCommand(
                "ALTER TABLE users ADD COLUMN IF NOT EXISTS sms_notifications BOOLEAN DEFAULT true AFTER email_notifications;", 
                connection);
            await cmd2.ExecuteNonQueryAsync();
            Console.WriteLine("Added/verified sms_notifications column.");

            // Describe the table
            var cmd3 = new MySqlCommand("DESCRIBE users;", connection);
            await using var reader = await cmd3.ExecuteReaderAsync();
            
            Console.WriteLine("\nusers table structure:");
            Console.WriteLine("======================");
            
            for (int i = 0; i < reader.FieldCount; i++)
            {
                Console.Write(reader.GetName(i) + "\t");
            }
            Console.WriteLine("\n----------------------");
            
            while (await reader.ReadAsync())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Console.Write(reader.GetValue(i) + "\t");
                }
                Console.WriteLine();
            }
        }
    }
}
