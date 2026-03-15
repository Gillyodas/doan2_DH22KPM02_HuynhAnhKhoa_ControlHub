---
config:
  layout: elk
---
classDiagram
    class AggregateRoot {
        <<abstract>>
        #RaiseDomainEvent()
    }
    class ValueObject {
        <<abstract>>
        Equality by value
    }
    class Role {
        +Guid Id
        +string Name
        +string Description
        +bool IsActive
        +bool IsDeleted
        ---
        +Create(name, description) Role
        +Update(name, description) Result
        +AddPermission(Permission) Result
        +ClearPermissions()
        +Activate()
        +Deactivate()
        +Delete()
    }
    class Permission {
        +Guid Id
        +string Code
        +string Description
        ---
        +Create(code, description) Result~Permission~
        +Update(code, description) Result
    }
    class Account {
        +Guid Id
        +Password Password
        +bool IsActive
        +bool IsDeleted
        +Guid RoleId
        ---
        +Create(password, roleId) Account
        +AddIdentifier(Identifier) Result
        +RemoveIdentifier(type, value) Result
        +AttachUser(User) Result
        +AttachRole(Role) Result
        +UpdatePassword(Password) Result
        +Activate()
        +Deactivate()
        +Delete()
    }
    class User {
        +Guid Id
        +string Username
        +string FirstName
        +string LastName
        +string PhoneNumber
        +bool IsDeleted
        ---
        +SetUsername(name)
        +UpdateProfile(firstName, lastName, phone)
        +Delete()
    }
    class Identifier {
        <<Value Object>>
        +IdentifierType Type
        +string Name
        +string Value
        +string NormalizedValue
        +bool IsDeleted
    }
    class Password {
        <<Value Object>>
        +byte[] Hash
        +byte[] Salt
        ---
        +Create(rawPassword, hasher) Result~Password~
        +IsWeak(rawPassword) bool
    }
    class Email {
        <<Value Object>>
        +string Value
        ---
        +Create(value) Result~Email~
    }
    class IdentifierConfig {
        +Guid Id
        +string Name
        +string Description
        +bool IsActive
        +DateTime CreatedAt
        ---
        +Create(name, description) IdentifierConfig
        +AddRule(type, parameters) Result
        +RemoveRule(ruleId) Result
        +Activate()
        +Deactivate()
        +UpdateRules(rules)
    }
    class ValidationRule {
        <<Value Object>>
        +Guid Id
        +ValidationRuleType Type
        +string ParametersJson
        +string ErrorMessage
        +int Order
    }
    class Token {
        +Guid Id
        +Guid AccountId
        +string Value
        +TokenType Type
        +DateTime ExpiredAt
        +bool IsUsed
        +bool IsRevoked
        ---
        +Create(accountId, value, type, expiry) Token
        +MarkAsUsed() Result
        +IsValid() bool
        +Revoke() Result
    }
    class IdentifierType {
        <<enumeration>>
        Email
        Phone
        Username
        Custom
    }
    class TokenType {
        <<enumeration>>
        ResetPassword
        VerifyEmail
        RefreshToken
        AccessToken
        RegisterUser
    }
    class ValidationRuleType {
        <<enumeration>>
        Required
        MinLength
        MaxLength
        Pattern
        Range
        Custom
    }
    AggregateRoot <|-- Account
    AggregateRoot <|-- Role
    AggregateRoot <|-- Token
    ValueObject <|-- Password
    ValueObject <|-- Identifier
    ValueObject <|-- Email
    ValueObject <|-- ValidationRule
    Account "1" *-- "0..*" Identifier : owns
    Account "1" *-- "1" Password : owns
    Account "1" *-- "0..1" User : owns
    Role "1" *-- "0..*" Permission : owns
    IdentifierConfig "1" *-- "0..*" ValidationRule : owns
    Account "0..*" --> "1" Role : assigned to
    Token "0..*" --> "1" Account : belongs to
    Identifier ..> IdentifierType : uses
    Token ..> TokenType : uses
    ValidationRule ..> ValidationRuleType : uses