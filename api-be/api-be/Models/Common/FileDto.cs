﻿namespace api_be.Models.Common
{
    public class FileDto
    {
        public string? Name { get; set; }

        public string? Path { get; set; }

        public string? Type { get; set; }

        public long? SizeInBytes { get; set; }
    }
}
