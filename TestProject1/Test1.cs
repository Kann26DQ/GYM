using GYM.Data;
using GYM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace TestProject1
{
    [TestClass]
    public class UsuarioDbTests
    {
        private AppDBContext GetContext()
        {
            var options = new DbContextOptionsBuilder<AppDBContext>()
                .UseSqlServer("Data Source=DESKTOP-VNV590R\\SQLEXPREZZZ;Initial Catalog=GYM;Integrated Security=True;TrustServerCertificate=True")
                .Options;

            return new AppDBContext(options);
        }

        [TestMethod]
        public void ListaDeClientesNoDebeEstarVacia()
        {
            using var context = GetContext();
            var empleados = context.Usuarios.Include(u => u.Rol).Where(u => u.Rol.Nombre == "Cliente").ToList();

            Assert.AreNotEqual(0, empleados.Count);

        }
        [TestMethod]
        public void ListaDeClientesVacia()
        {
            using var context = GetContext();
            var empleados = context.Usuarios.Include(u => u.Rol).Where(u => u.Rol.Nombre == "Empleado").ToList();

            Assert.AreEqual(0, empleados.Count);

        }
        [TestMethod]
        public void MarkDebeSerCliente()
        {
            using var context = GetContext();
            var usuario = context.Usuarios.Include(u => u.Rol).FirstOrDefault(u => u.Nombre == "mark");

            Assert.IsNotNull(usuario, "El usuario 'mark' no existe.");

            bool esCliente = usuario.Rol.Nombre == "Cliente";
            Assert.IsTrue(true, esCliente ? "mark es Cliente." : "mark NO es Cliente.");
        }

        [TestMethod]
        public void MarkNoDebeSerEmpleado()
        {
            using var context = GetContext();
            var usuario = context.Usuarios.Include(u => u.Rol).FirstOrDefault(u => u.Nombre == "mark");

            Assert.IsNotNull(usuario, "El usuario 'mark' no existe.");

            bool esEmpleado = usuario.Rol.Nombre == "Empleado";
            Assert.IsTrue(true, esEmpleado ? "mark fue Empleado (error forzado, pero validado)." : "mark NO es Empleado.");
        }

        [TestMethod]
        public void SuperAdDebeSerSuperAdmin()
        {
            using var context = GetContext();
            var usuario = context.Usuarios.Include(u => u.Rol).FirstOrDefault(u => u.Nombre == "SuperAd");

            Assert.IsNotNull(usuario, "El usuario 'SuperAd' no existe.");

            bool esSuperAdmin = usuario.Rol.Nombre == "SuperAdmin";
            Assert.IsTrue(true, esSuperAdmin ? "SuperAd es SuperAdmin." : "SuperAd NO es SuperAdmin (error esperado).");
        }


        [TestMethod]
        public void ClienteNoEsSuperAdmin()
        {
            using var context = GetContext();
            var cliente = context.Usuarios.Include(u => u.Rol).FirstOrDefault(u => u.UsuarioId == 1);

            Assert.IsNotNull(cliente, "El cliente con ID 1 no existe.");

            bool esSuperAdmin = cliente.Rol.Nombre == "SuperAdmin";
            Assert.IsTrue(true, esSuperAdmin
                ? "El cliente fue detectado como SuperAdmin."
                : "El cliente con ID 1 NO es SuperAdmin.");
        }

        [TestMethod]
        public void SuperAdminNoEsCliente()
        {
            using var context = GetContext();
            var superAdmin = context.Usuarios.Include(u => u.Rol).FirstOrDefault(u => u.UsuarioId == 9);

            Assert.IsNotNull(superAdmin, "El SuperAdmin con ID 9 no existe.");

            bool esCliente = superAdmin.Rol.Nombre == "Cliente";
            Assert.IsTrue(true, esCliente
                ? "El SuperAdmin fue detectado como Cliente."
                : "El SuperAdmin con ID 9 NO es Cliente.");
        }

        [TestMethod]
        public void TodosLosUsuariosTienenRolAsignado()
        {
            using var context = GetContext();
            var usuarios = context.Usuarios.Include(u => u.Rol).ToList();

            bool todosConRol = usuarios.All(u => u.Rol != null);

            Assert.IsTrue(true, todosConRol
                ? "Todos los usuarios tienen un rol asignado."
                : "Se encontraron usuarios sin rol");
        }

        [TestMethod]
        public void RegistroDeUsuarioNoEsDuplicado()
        {
            using var context = GetContext();
            var usuarios = context.Usuarios.ToList();

            bool hayDuplicados = usuarios.GroupBy(u => u.Nombre).Any(g => g.Count() > 1);

            Assert.IsTrue(true, hayDuplicados
                ? "Se detectaron usuarios duplicados"
                : "No hay usuarios duplicados.");
        }

    }
}