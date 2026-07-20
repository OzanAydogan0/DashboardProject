namespace dashboardapi.DTOs;

// --- CUSTOMER (MÜŞTERİ) DTO'ları ---
public record CustomerDto(
    string CustomerId, 
    string CustomerName, 
    string CustomerType, 
    string CustomerStatus
);

public record CreateCustomerRequest(
    string CustomerName, 
    string CustomerType, 
    string CustomerStatus
);

// --- PROGRAM (PORTFÖY) DTO'ları ---
public record ProgramDto(
    string ProgramId, 
    string ProgramName, 
    string? ProgramDescription, 
    string ProgramStatus
);

public record CreateProgramRequest(
    string ProgramName, 
    string? ProgramDescription, 
    string ProgramStatus
);