using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Should;
using Xunit;

namespace ReflectItEasy
{
    public class User
    {

    }

    public interface IBusinessResponse<T>
    {

    }

    public interface IExampleService
    {
        IBusinessResponse<User> GetUser();
        IBusinessResponse<List<User>> GetUsers();
        IBusinessResponse<IEnumerable<User>> GetUsersEx();
        IBusinessResponse<List<User>> GetUsersEx2();
        IBusinessResponse<ICollection<User>> GetUsersEx3();
    }

    public class ExampleService : IExampleService
    {
        public IBusinessResponse<User> GetUser()
        {
            return null;
        }

        public IBusinessResponse<List<User>> GetUsers()
        {
            return null;
        }

        public IBusinessResponse<IEnumerable<User>> GetUsersEx()
        {
            return null;
        }

        public IBusinessResponse<List<User>> GetUsersEx2()
        {
            return null;
        }

        public IBusinessResponse<ICollection<User>> GetUsersEx3()
        {
            return null;
        }
    }

    public static class Reflector
    {
        public static bool IsGenericEnumerable(this Type type)
        {
            var genericArgs = type.GetGenericArguments();
            return genericArgs.Length == 1 && typeof(IEnumerable<>).MakeGenericType(genericArgs).IsAssignableFrom(type);
        }

        public static List<MethodInfo> GetMethodsThatReturnIBusinessResponseWithList()
        {
            var methods =
                typeof(IExampleService).GetMethods()
                    .Where(x => x.ReturnType.IsGenericType &&
                        x.ReturnType.GetGenericTypeDefinition() == typeof(IBusinessResponse<>) &&
                        x.ReturnType.GetGenericArguments().Length == 1 &&
                        x.ReturnType.GetGenericArguments()[0].IsGenericEnumerable())
                    .ToList();

            return methods;
        }
    }

    public class Tests
    {
        [Fact]
        public void Interface_Methods_Should_Return_IBusinessResponse()
        {
            var methodInfo = typeof(IExampleService).GetMethod("GetUser");
            var returnType = methodInfo.ReturnType;

            returnType.IsGenericType.ShouldBeTrue();
            returnType.GetGenericTypeDefinition().ShouldEqual(typeof(IBusinessResponse<>));
        }

        [Fact]
        public void Should_Return_Only_Methods_That_Returns_Any_Kind_Of_Generic_List()
        {
            var methods = Reflector.GetMethodsThatReturnIBusinessResponseWithList();

            var methodNames = methods.Select(x => x.Name).ToList();

            methods.Count().ShouldEqual(4);
            methodNames.ShouldContain("GetUsers");
            methodNames.ShouldContain("GetUsersEx");
            methodNames.ShouldContain("GetUsersEx2");
            methodNames.ShouldContain("GetUsersEx3");
        }
    }
}
