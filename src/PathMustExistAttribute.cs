using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace DeepRen
{
    internal class PathMustExistAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            string path = value.ToString();
            return Directory.Exists(path);
        }
    }
}