using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using FakeItEasy;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.Kernel;
using Should;
using Xunit;

namespace ReflectItEasy
{
    public class User
    {

    }

    public interface IBusinessResponse<T>
    {
        T Data { get; }
    }

    public class BusinessResponse<T> : IBusinessResponse<T>
    {
        public BusinessResponse(T data)
        {
            Data = data;
        }

        public T Data { get; private set; }
    }

    public interface IExampleService
    {
        IBusinessResponse<User> GetUser();
        IBusinessResponse<List<User>> GetUsers(int id);
        IBusinessResponse<IEnumerable<User>> GetUsersEx(int id);
        IBusinessResponse<List<User>> GetUsersEx2(int id);
        IBusinessResponse<ICollection<User>> GetUsersEx3(int id);
    }

    public interface IRemotingFacade
    {
        IEnumerable<User> Users();
    }


    public class ExampleService : IExampleService
    {
        private readonly IRemotingFacade _facade;

        public ExampleService(IRemotingFacade facade)
        {
            _facade = facade;
        }

        public IBusinessResponse<User> GetUser()
        {
            return null;
        }

        public IBusinessResponse<List<User>> GetUsers(int id)
        {
            var enumerable = _facade.Users();

            return new BusinessResponse<List<User>>(enumerable.ToList());
        }

        public IBusinessResponse<IEnumerable<User>> GetUsersEx(int id)
        {
            return new BusinessResponse<IEnumerable<User>>(_facade.Users());
        }

        public IBusinessResponse<List<User>> GetUsersEx2(int id)
        {
            return new BusinessResponse<List<User>>(_facade.Users().ToList());
        }

        public IBusinessResponse<ICollection<User>> GetUsersEx3(int id)
        {
            return new BusinessResponse<ICollection<User>>(new Collection<User>(_facade.Users().ToList()));
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
                typeof(ExampleService).GetMethods()
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
          
            
            //var guardClauseAssertion = new GuardClauseAssertion(fixture);
            //guardClauseAssertion.Verify(methods);

           
            var methods = Reflector.GetMethodsThatReturnIBusinessResponseWithList();

            var remotingFacade = A.Fake<IRemotingFacade>();
            var exampleService = new ExampleService(remotingFacade);
            
            A.CallTo(() => remotingFacade.Users()).Returns(null);

            foreach (var methodInfo in methods)
            {
                var parameterValues = CreateParameterValues(methodInfo.GetParameters());
                var returnObject = methodInfo.Invoke(exampleService, parameterValues);

                var value = returnObject.GetType().GetProperty("Data").GetValue(returnObject);
                
            }

            var methodNames = methods.Select(x => x.Name).ToList();

            methods.Count().ShouldEqual(4);
            methodNames.ShouldContain("GetUsers");
            methodNames.ShouldContain("GetUsersEx");
            methodNames.ShouldContain("GetUsersEx2");
            methodNames.ShouldContain("GetUsersEx3");
        }

        public object[] CreateParameterValues(ParameterInfo[] parameterInfos)
        {
            var fixture = new Fixture();
            return parameterInfos.Select(x =>
            {
                var context = new SpecimenContext(fixture);
                var value = context.Resolve(new SeededRequest(x.ParameterType, null));
                return value;
            }).ToArray();
        }

    }
}
