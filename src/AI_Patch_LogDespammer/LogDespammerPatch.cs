using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace AI_Fixes
{
    public static class LogDespammerPatch
    {
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

        public static void Patch(AssemblyDefinition mainAss)
        {
            var typeDefinition = new TypeDefinition("LOG_SPAM_PATCH", "LogStub", TypeAttributes.Class | TypeAttributes.NotPublic);
            typeDefinition.BaseType = mainAss.MainModule.ImportReference(typeof(object));

            var methodDefinition = new MethodDefinition("LogError", MethodAttributes.Static | MethodAttributes.Public, mainAss.MainModule.ImportReference(typeof(void)));
            methodDefinition.Parameters.Add(new ParameterDefinition(mainAss.MainModule.ImportReference(typeof(object))));
            methodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            typeDefinition.Methods.Add(methodDefinition);
            mainAss.MainModule.Types.Add(typeDefinition);

            foreach (var memberReference in mainAss.MainModule.GetMemberReferences())
            {
                if (memberReference is MethodReference methodReference &&
                    memberReference.DeclaringType.Name.Equals("Debug", StringComparison.Ordinal) &&
                    memberReference.DeclaringType.Namespace.Equals("UnityEngine", StringComparison.Ordinal))
                {
                    switch (methodReference.Name)
                    {
                        case "LogError":
                        case "Log":
                            if (methodReference.Parameters.Count == 1)
                            {
                                methodReference.DeclaringType = methodDefinition.DeclaringType;
                                methodReference.Name = methodDefinition.Name;
                            }
                            break;
                    }
                }
            }
        }

    }
}