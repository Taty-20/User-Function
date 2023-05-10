using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace User_Function.Models
{
    public class UserCredentials
    {
        public string? User { get; set; }

        public string? Password { get; set; }
    }
    internal class UsuarioModel
    {
        public int Id { get; set; }

        public string? Nombre { get; set; }

        public string? Apellido { get; set; }

        public string? Correo { get; set; }

        public string? Contraseña { get; set; }
    }

    public class ListUserModel
    {
        public int Id { get; set; }

        public string? Nombre { get; set; }

        public string? Apellido { get; set; }

        public string? Correo { get; set; }
    }

    public class CreateUserModel
    {
        public string? Nombre { get; set; }

        public string? Apellido { get; set; }

        public string? Correo { get; set; }

        public string? Contraseña { get; set; }
    }
}
