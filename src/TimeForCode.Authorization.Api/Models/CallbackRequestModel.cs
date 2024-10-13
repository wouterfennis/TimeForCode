using System.ComponentModel.DataAnnotations;

public class CallbackRequestModel
    {
        [Required]
        public required string Code { get; init; }

        [Required]
        public required string State { get; init; }
    }