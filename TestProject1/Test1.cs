using System;
using System.Linq;
using System.Threading.Tasks;
using GYM.Controllers;
using GYM.Data;
using GYM.Models;
using GYM.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestProject1
{
    [TestClass]
    public class GestionUsuariosControllerTests
    {
        private static AppDBContext BuildContext(string DBGYMPRO)
        {
            var options = new DbContextOptionsBuilder<AppDBContext>()
                .UseInMemoryDatabase(databaseName: DBGYMPRO)
                .EnableSensitiveDataLogging()
                .Options;

            var ctx = new AppDBContext(options);

            // Semilla mínima de Roles (por si el HasData no se aplica en InMemory)
            if (!ctx.Roles.Any())
            {
                ctx.Roles.AddRange(
                    new Rol { RolId = 1, Nombre = "Cliente" },
                    new Rol { RolId = 2, Nombre = "Empleado" },
                    new Rol { RolId = 3, Nombre = "SuperAdmin" }
                );
                ctx.SaveChanges();
            }

            return ctx;
        }

        [TestMethod]
        public async Task Create_ModelStateInvalido_RetornaVistaConErrores()
        {
            var ctx = BuildContext(nameof(Create_ModelStateInvalido_RetornaVistaConErrores));
            var controller = new GestionUsuariosController(ctx);

            var vm = new UsuarioCreateVM
            {
                // Forzar error: nombre vacío
                Nombre = "",
                Email = "user@correo.com",
                Password = "12345678",
                ConfirmarPassword = "12345678",
                Telefono = "123456789",
                RolId = 1
            };
            controller.ModelState.AddModelError("Nombre", "El nombre es obligatorio");

            var result = await controller.Create(vm);

            var view = result as ViewResult;
            Assert.IsNotNull(view);
            Assert.AreEqual("~/Views/SuperAdmin/GestionUsuarios/Create.cshtml", view.ViewName);
            Assert.IsFalse(controller.ModelState.IsValid);
            Assert.AreSame(vm, view.Model);
        }

        [TestMethod]
        public async Task Create_EmailDuplicado_MuestraErrorYNoCrea()
        {
            var ctx = BuildContext(nameof(Create_EmailDuplicado_MuestraErrorYNoCrea));
            ctx.Usuarios.Add(new Usuario
            {
                Nombre = "Existente",
                Email = "dup@correo.com",
                Password = "HASH",
                Telefono = "900000001",
                FechaRegistro = DateTime.Now,
                RolId = 1,
                Activo = true
            });
            await ctx.SaveChangesAsync();

            var controller = new GestionUsuariosController(ctx);
            var vm = new UsuarioCreateVM
            {
                Nombre = "Nuevo",
                Email = "dup@correo.com", // Duplicado
                Password = "12345678",
                ConfirmarPassword = "12345678",
                Telefono = "900000002",
                RolId = 1
            };

            var result = await controller.Create(vm);

            var view = result as ViewResult;
            Assert.IsNotNull(view);
            Assert.AreEqual("~/Views/SuperAdmin/GestionUsuarios/Create.cshtml", view.ViewName);
            Assert.IsTrue(controller.ModelState.ContainsKey(nameof(vm.Email)));
            Assert.AreEqual(1, ctx.Usuarios.Count(u => u.Email == "dup@correo.com")); // no se crea otro
        }

        [TestMethod]
        public async Task Create_Valido_CreaUsuarioHasheado_YRedirigeIndex()
        {
            var ctx = BuildContext(nameof(Create_Valido_CreaUsuarioHasheado_YRedirigeIndex));
            var controller = new GestionUsuariosController(ctx);
            var plain = "12345678";

            var vm = new UsuarioCreateVM
            {
                Nombre = "Valido",
                Email = "valido@correo.com",
                Password = plain,
                ConfirmarPassword = plain,
                Telefono = "987654321",
                RolId = 1
            };

            var result = await controller.Create(vm);

            var redirect = result as RedirectToActionResult;
            Assert.IsNotNull(redirect);
            Assert.AreEqual(nameof(GestionUsuariosController.Index), redirect.ActionName);

            var creado = await ctx.Usuarios.AsNoTracking().SingleAsync(u => u.Email == vm.Email);
            Assert.AreEqual(vm.Nombre, creado.Nombre);
            Assert.IsTrue(creado.Activo);
            Assert.AreNotEqual(plain, creado.Password); // hasheado
        }

        [TestMethod]
        public async Task CheckEmailUnique_Existente_RetornaFalse()
        {
            var ctx = BuildContext(nameof(CheckEmailUnique_Existente_RetornaFalse));
            ctx.Usuarios.Add(new Usuario
            {
                Nombre = "Existente",
                Email = "existe@correo.com",
                Password = "HASH",
                Telefono = "900000001",
                FechaRegistro = DateTime.Now,
                RolId = 1,
                Activo = true
            });
            await ctx.SaveChangesAsync();

            var controller = new GestionUsuariosController(ctx);

            var result = await controller.CheckEmailUnique("existe@correo.com", null) as JsonResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(false, result.Value);
        }

        [TestMethod]
        public async Task Edit_EmailDuplicado_MuestraErrorYNoActualiza()
        {
            var ctx = BuildContext(nameof(Edit_EmailDuplicado_MuestraErrorYNoActualiza));
            var a = new Usuario
            {
                Nombre = "A",
                Email = "a@correo.com",
                Password = "HASH",
                Telefono = "900000001",
                FechaRegistro = DateTime.Now,
                RolId = 1,
                Activo = true
            };
            var b = new Usuario
            {
                Nombre = "B",
                Email = "b@correo.com",
                Password = "HASH",
                Telefono = "900000002",
                FechaRegistro = DateTime.Now,
                RolId = 1,
                Activo = true
            };
            ctx.Usuarios.AddRange(a, b);
            await ctx.SaveChangesAsync();

            var controller = new GestionUsuariosController(ctx);
            var vm = new UsuarioEditVM
            {
                UsuarioId = a.UsuarioId,
                Nombre = "A Edit",
                Email = "b@correo.com", // colisiona con B
                Telefono = "900000003",
                RolId = 2,
                Activo = false
            };

            var result = await controller.Edit(vm);

            var view = result as ViewResult;
            Assert.IsNotNull(view);
            Assert.AreEqual("~/Views/SuperAdmin/GestionUsuarios/Edit.cshtml", view.ViewName);
            Assert.IsTrue(controller.ModelState.ContainsKey(nameof(vm.Email)));

            var aDb = await ctx.Usuarios.FindAsync(a.UsuarioId);
            Assert.AreEqual("a@correo.com", aDb!.Email); // sin cambios
        }

        [TestMethod]
        public async Task Edit_Valido_ActualizaCampos_YRedirige()
        {
            var ctx = BuildContext(nameof(Edit_Valido_ActualizaCampos_YRedirige));
            var user = new Usuario
            {
                Nombre = "Original",
                Email = "orig@correo.com",
                Password = "HASH",
                Telefono = "900000000",
                FechaRegistro = DateTime.Now,
                RolId = 1,
                Activo = true
            };
            ctx.Usuarios.Add(user);
            await ctx.SaveChangesAsync();

            var controller = new GestionUsuariosController(ctx);
            var vm = new UsuarioEditVM
            {
                UsuarioId = user.UsuarioId,
                Nombre = "Nuevo",
                Email = "nuevo@correo.com",
                Telefono = "999999999",
                RolId = 2,
                Activo = false,
                NewPassword = "NuevaClave123"
            };

            var result = await controller.Edit(vm);

            var redirect = result as RedirectToActionResult;
            Assert.IsNotNull(redirect);
            Assert.AreEqual(nameof(GestionUsuariosController.Index), redirect.ActionName);

            var updated = await ctx.Usuarios.FindAsync(user.UsuarioId);
            Assert.AreEqual("Nuevo", updated!.Nombre);
            Assert.AreEqual("nuevo@correo.com", updated.Email);
            Assert.AreEqual("999999999", updated.Telefono);
            Assert.AreEqual(2, updated.RolId);
            Assert.IsFalse(updated.Activo);
            Assert.AreNotEqual("NuevaClave123", updated.Password); // hasheada
        }

        [TestMethod]
        public async Task ToggleActivo_IdNoExiste_NotFound()
        {
            var ctx = BuildContext(nameof(ToggleActivo_IdNoExiste_NotFound));
            var controller = new GestionUsuariosController(ctx);

            var result = await controller.ToggleActivo(99999);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }
    }
}