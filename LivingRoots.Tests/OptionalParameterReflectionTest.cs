using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace LivingRoots.Tests
{
    public class OptionalParameterReflectionTest
    {
        /// <summary>
        /// Test class with a constructor that has optional parameters with specific default values.
        /// This will be used to verify that the CreateInstanceWithFallback method correctly handles
        /// optional parameters using DefaultValue instead of Type.Missing.
        /// </summary>
        private class TypeWithOptionalParametersAndDefaults
        {
            public string StringParam { get; }
            public int IntParam { get; }
            public double DoubleParam { get; }
            public bool BoolParam { get; }

            public TypeWithOptionalParametersAndDefaults(
                string stringParam = "default_string", 
                int intParam = 123, 
                double doubleParam = 45.67, 
                bool boolParam = true)
            {
                StringParam = stringParam;
                IntParam = intParam;
                DoubleParam = doubleParam;
                BoolParam = boolParam;
            }
        }

        /// <summary>
        /// Creates an instance of type T using reflection with fallback strategies.
        /// This is the updated version that uses p.DefaultValue for both optional and default parameters.
        /// </summary>
        /// <typeparam name="T">The type to instantiate.</typeparam>
        /// <returns>An instance of type T.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no valid constructor is found.</exception>
        private static T CreateInstanceWithFallbackFixed<T>() where T : class
        {
            Exception? lastError = null;

            // First try: Activator.CreateInstance with nonPublic: true
            try
            {
                var instance = Activator.CreateInstance(typeof(T), nonPublic: true) as T;
                if (instance != null)
                {
                    return instance;
                }
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            // Second try: Find a constructor with optional/default parameters
            var constructors = typeof(T).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                
                // Skip constructors with any required reference-type parameter (can't safely provide a value)
                if (parameters.Any(p =>
                        !p.IsOptional &&
                        !p.HasDefaultValue &&
                        !p.ParameterType.IsValueType))
                {
                    continue;
                }

                try
                {
                    var args = constructor.GetParameters().Select(p =>
                    {
                        // Optional parameters in C# always have a default value; prefer DefaultValue over Type.Missing.
                        if (p.IsOptional || p.HasDefaultValue)
                            return p.DefaultValue;

                        if (p.ParameterType.IsValueType)
                            return Activator.CreateInstance(p.ParameterType);

                        // Should be unreachable due to the "required reference-type parameter" skip above,
                        // but keep a safe fallback.
                        return null;
                    }).ToArray();

                    // Try to invoke the constructor with the default arguments
                    var instance = constructor.Invoke(args) as T;
                    if (instance != null)
                    {
                        return instance;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    // Try to next constructor
                    continue;
                }
            }

            // If all attempts fail, throw an informative exception with the last error as InnerException
            throw new InvalidOperationException(
                $"Failed to create instance of type {typeof(T)} for tests. " +
                $"Tried Activator.CreateInstance and all constructors with default/optional parameters via reflection. " +
                $"Ensure that type has an accessible constructor with default/optional parameters or provide a test-specific factory method.",
                lastError);
        }

        /// <summary>
        /// Creates an instance of type T using reflection with fallback strategies.
        /// This is the original version that uses Type.Missing for optional parameters.
        /// </summary>
        /// <typeparam name="T">The type to instantiate.</typeparam>
        /// <returns>An instance of type T.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no valid constructor is found.</exception>
        private static T CreateInstanceWithFallbackOriginal<T>() where T : class
        {
            Exception? lastError = null;

            // First try: Activator.CreateInstance with nonPublic: true
            try
            {
                var instance = Activator.CreateInstance(typeof(T), nonPublic: true) as T;
                if (instance != null)
                {
                    return instance;
                }
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            // Second try: Find a constructor with optional/default parameters
            var constructors = typeof(T).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                
                // Skip constructors with any required reference-type parameter (can't safely provide a value)
                if (parameters.Any(p =>
                        !p.IsOptional &&
                        !p.HasDefaultValue &&
                        !p.ParameterType.IsValueType))
                {
                    continue;
                }

                try
                {
                    var args = constructor.GetParameters().Select(p =>
                    {
                        if (p.IsOptional)
                            return Type.Missing;
                        if (p.HasDefaultValue)
                            return p.DefaultValue;
                        if (p.ParameterType.IsValueType)
                            return Activator.CreateInstance(p.ParameterType);
                        return Type.Missing;
                    }).ToArray();

                    // Try to invoke the constructor with the default arguments
                    var instance = constructor.Invoke(args) as T;
                    if (instance != null)
                    {
                        return instance;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    // Try to next constructor
                    continue;
                }
            }

            // If all attempts fail, throw an informative exception with the last error as InnerException
            throw new InvalidOperationException(
                $"Failed to create instance of type {typeof(T)} for tests. " +
                $"Tried Activator.CreateInstance and all constructors with default/optional parameters via reflection. " +
                $"Ensure that type has an accessible constructor with default/optional parameters or provide a test-specific factory method.",
                lastError);
        }

        [Fact]
        public void CreateInstanceWithFallback_WithOptionalParameters_UsesDefaultValueInsteadOfTypeMissing()
        {
            // This test verifies that the fixed implementation correctly handles optional parameters
            // using DefaultValue instead of Type.Missing
            // Arrange & Act
            var instance = CreateInstanceWithFallbackFixed<TypeWithOptionalParametersAndDefaults>();

            // Assert - Verify that the instance was created with the default values
            Assert.NotNull(instance);
            Assert.Equal("default_string", instance.StringParam);
            Assert.Equal(123, instance.IntParam);
            Assert.Equal(45.67, instance.DoubleParam);
            Assert.True(instance.BoolParam);
        }

        [Fact]
        public void ParameterReflection_WhenParameterIsOptional_HasDefaultValue()
        {
            // This test verifies the relationship between IsOptional and HasDefaultValue
            var constructor = typeof(TypeWithOptionalParametersAndDefaults).GetConstructor(new[] {
                typeof(string), typeof(int), typeof(double), typeof(bool)
            });
            
            Assert.NotNull(constructor);
            
            var parameters = constructor.GetParameters();
            Assert.Equal(4, parameters.Length);

            // All parameters in this constructor are optional with default values
            foreach (var param in parameters)
            {
                Assert.True(param.IsOptional || param.HasDefaultValue, 
                    $"Parameter {param.Name} should be either optional or have a default value");
                
                // For optional parameters in C#, they should have default values
                Assert.NotNull(param.DefaultValue);
            }
        }
    }
}