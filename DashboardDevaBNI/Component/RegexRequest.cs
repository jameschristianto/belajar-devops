using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DashboardDevaBNI.Component
{
	public class RegexRequest
	{

		public static async Task<bool> RegexValidation(Object data)
		{
			try
			{
				var pattern = new Regex(@"[<>]");
				Type objectType = data.GetType();
				PropertyInfo[] properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
				foreach (PropertyInfo property in properties)
				{
					if (property.PropertyType.Name == "String" && property.GetValue(data) != null)
					{
						if (pattern.IsMatch(property.GetValue(data).ToString()) == true)
						{
							return false;
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		public static bool RegexSQLInjection(SqlParameter[] data)
		{
			try
			{
				foreach (SqlParameter property in data)
				{
					if (property.Value is not null && (property.Value.ToString().Contains(" AND ") || property.Value.ToString().Contains(" OR ") || property.Value.ToString().Contains(" and ") || property.Value.ToString().Contains(" or ")))
					{
						return false;
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		public static async Task<bool> RegexSQLInjectionAsync(SqlParameter[] data)
		{
			try
			{
				foreach (SqlParameter property in data)
				{
					if (property.Value is not null && (property.Value.ToString().Contains(" AND ") || property.Value.ToString().Contains(" OR ") || property.Value.ToString().Contains(" and ") || property.Value.ToString().Contains(" or ")))
					{
						return false;
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		public static bool ValidatePassword(string password)
		{
			Regex uppercaseRegex = new Regex("[A-Z]");
			Regex numbersRegex = new Regex("[0-9]");
			Regex specialCharsRegex = new Regex("[!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~]");

			MatchCollection uppercaseLetters = uppercaseRegex.Matches(password);
			MatchCollection numbers = numbersRegex.Matches(password);
			MatchCollection specialCharacters = specialCharsRegex.Matches(password);

			if (password.Length < 8 || password.Length > 15)
			{
				Console.WriteLine("Kata sandi harus terdiri dari 8 hingga 15 karakter.");
				return false;
			}
			else if (uppercaseLetters.Count < 2)
			{
				Console.WriteLine("Kata sandi harus memiliki minimal dua huruf besar.");
				return false;
			}
			else if (numbers.Count < 2)
			{
				Console.WriteLine("Kata sandi harus memiliki minimal dua angka.");
				return false;
			}
			else if (specialCharacters.Count < 2)
			{
				Console.WriteLine("Kata sandi harus memiliki minimal dua karakter khusus.");
				return false;
			}

			return true;
		}

	}
}
