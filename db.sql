-- =====================================================
-- LOCADORA AUTO - SCRIPT FINAL CORRIGIDO (MVP)
-- MariaDB / MySQL 10.4+
-- =====================================================

SET FOREIGN_KEY_CHECKS = 0;

CREATE SCHEMA Locadora_autos
DEFAULT CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE Locadora_autos;

CREATE TABLE manutencao (
    id_manutencao INT AUTO_INCREMENT PRIMARY KEY,
    id_veiculo INT NOT NULL,
    tipo VARCHAR(50),
    descricao TEXT,
    custo DECIMAL(10,2),
    data_inicio DATETIME,
    data_fim DATETIME,
    status VARCHAR(20),
    FOREIGN KEY (id_veiculo) REFERENCES veiculo(id_veiculo)
);

CREATE TABLE auditoria (
    id INT AUTO_INCREMENT PRIMARY KEY,
    tabela VARCHAR(50),
    id_registro INT,
    acao VARCHAR(20), -- INSERT, UPDATE, DELETE
    usuario_id VARCHAR(255),
    data_evento DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- =====================================================
-- 1-->AUTH / IDENTITY
-- =====================================================

CREATE TABLE AspNetUsers (
    Id VARCHAR(255) NOT NULL PRIMARY KEY,
    UserName VARCHAR(256) NULL,
    NormalizedUserName VARCHAR(256) NULL UNIQUE,
    Email VARCHAR(256) NULL,
    NormalizedEmail VARCHAR(256) NULL,
    EmailConfirmed BIT(1) NOT NULL DEFAULT 0,
    PasswordHash LONGTEXT NULL,
    SecurityStamp LONGTEXT NULL,
    ConcurrencyStamp VARCHAR(36) NULL,
    PhoneNumber VARCHAR(50) NULL,
    PhoneNumberConfirmed BIT(1) NOT NULL DEFAULT 0,
    TwoFactorEnabled BIT(1) NOT NULL DEFAULT 0,
    LockoutEnd DATETIME(6) NULL,
    LockoutEnabled BIT(1) NOT NULL DEFAULT 0,
    AccessFailedCount INT NOT NULL DEFAULT 0,
    NomeCompleto VARCHAR(255) NULL,
    Cpf CHAR(11) NULL UNIQUE,
    Ativo BIT(1) NOT NULL DEFAULT 1,
    DataCriacao DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


CREATE TABLE refresh_tokens (
    id INT AUTO_INCREMENT PRIMARY KEY,
    token VARCHAR(512) NOT NULL UNIQUE,
    expira_em DATETIME NOT NULL,
    revogado BIT(1) DEFAULT 0,
    criado_em DATETIME DEFAULT CURRENT_TIMESTAMP,
    user_id VARCHAR(255) NOT NULL,
    FOREIGN KEY (user_id) REFERENCES aspnet_users(id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE AspNetRoles (
    Id VARCHAR(255) NOT NULL,
    Name VARCHAR(256),
    NormalizedName VARCHAR(256),
    ConcurrencyStamp LONGTEXT,
    PRIMARY KEY (Id),
    UNIQUE KEY RoleNameIndex (NormalizedName)
) ENGINE=InnoDB
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

CREATE TABLE AspNetUserRoles (
    UserId VARCHAR(255) NOT NULL,
    RoleId VARCHAR(255) NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_User
        FOREIGN KEY (UserId) REFERENCES aspnet_users(id)
        ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Role
        FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id)
        ON DELETE CASCADE
) ENGINE=InnoDB
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

CREATE TABLE AspNetUserClaims (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId VARCHAR(255) NOT NULL,
    ClaimType VARCHAR(256),
    ClaimValue VARCHAR(256),
    FOREIGN KEY (UserId) REFERENCES aspnet_users(id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE AspNetRoleClaims (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    RoleId VARCHAR(255) NOT NULL,
    ClaimType VARCHAR(256),
    ClaimValue VARCHAR(256),
    FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE AspNetUserLogins (
    LoginProvider VARCHAR(255) NOT NULL,
    ProviderKey VARCHAR(255) NOT NULL,
    ProviderDisplayName VARCHAR(255),
    UserId VARCHAR(255) NOT NULL,
    PRIMARY KEY (LoginProvider, ProviderKey),
    FOREIGN KEY (UserId) REFERENCES aspnet_users(Id) ON DELETE CASCADE
) ENGINE=InnoDB;



-- =====================================================
-- 2--> BASE
-- =====================================================

CREATE TABLE filial (
    id_filial INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    cidade VARCHAR(100) NOT NULL,
    ativo BIT(1) DEFAULT 1
) ENGINE=InnoDB;

CREATE TABLE cliente (
    id_cliente INT AUTO_INCREMENT PRIMARY KEY,
    cpf CHAR(11) NOT NULL UNIQUE,
    nome VARCHAR(150) NOT NULL,
    telefone VARCHAR(20),
    email VARCHAR(150),
    status VARCHAR(20) DEFAULT 'ATIVO'
) ENGINE=InnoDB;

CREATE TABLE endereco (
    id_endereco INT AUTO_INCREMENT PRIMARY KEY,
    id_cliente INT NOT NULL,
    logradouro VARCHAR(150),
    numero VARCHAR(20),
    complemento VARCHAR(100),
    bairro VARCHAR(100),
    cidade VARCHAR(100),
    estado CHAR(2),
    cep CHAR(8),
    FOREIGN KEY (id_cliente) REFERENCES cliente(id_cliente)
) ENGINE=InnoDB;

CREATE TABLE funcionario (
    id_funcionario INT AUTO_INCREMENT PRIMARY KEY,
    matricula VARCHAR(20) NOT NULL UNIQUE,
    nome VARCHAR(150) NOT NULL,
    cargo VARCHAR(50),
    status VARCHAR(20) DEFAULT 'ATIVO'
) ENGINE=InnoDB;

-- =====================================================
-- VEÍCULOS
-- =====================================================

CREATE TABLE categoria_veiculo (
    id_categoria INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(50) NOT NULL,
    valor_diaria DECIMAL(10,2) NOT NULL,
    limite_km INT,
    valor_km_excedente DECIMAL(10,2)
) ENGINE=InnoDB;

CREATE TABLE veiculo (
    id_veiculo INT AUTO_INCREMENT PRIMARY KEY,
    placa VARCHAR(10) NOT NULL UNIQUE,
    chassi VARCHAR(30) NOT NULL UNIQUE,
    id_categoria INT NOT NULL,
    km_atual INT NOT NULL,
    status VARCHAR(20) NOT NULL,
    id_filial_atual INT NOT NULL,
    FOREIGN KEY (id_categoria) REFERENCES categoria_veiculo(id_categoria),
    FOREIGN KEY (id_filial_atual) REFERENCES filial(id_filial)
) ENGINE=InnoDB;

-- =====================================================
-- OPERAÇÃO
-- =====================================================

CREATE TABLE locacao (
    id_locacao INT AUTO_INCREMENT PRIMARY KEY,
    id_cliente INT NOT NULL,
    id_veiculo INT NOT NULL,
    id_funcionario INT NOT NULL,
    id_filial_retirada INT NOT NULL,
    id_filial_devolucao INT,
    data_inicio DATETIME NOT NULL,
    data_fim_prevista DATETIME NOT NULL,
    data_fim_real DATETIME,
    km_inicial INT NOT NULL,
    km_final INT,
    valor_previsto DECIMAL(10,2) NOT NULL,
    valor_final DECIMAL(10,2),
    status VARCHAR(20) NOT NULL,
    FOREIGN KEY (id_cliente) REFERENCES cliente(id_cliente),
    FOREIGN KEY (id_veiculo) REFERENCES veiculo(id_veiculo),
    FOREIGN KEY (id_funcionario) REFERENCES funcionario(id_funcionario),
    FOREIGN KEY (id_filial_retirada) REFERENCES filial(id_filial),
    FOREIGN KEY (id_filial_devolucao) REFERENCES filial(id_filial)
) ENGINE=InnoDB;

-- =====================================================
-- FINANCEIRO
-- =====================================================

CREATE TABLE forma_pagamento (
    id_forma_pagamento INT AUTO_INCREMENT PRIMARY KEY,
    descricao VARCHAR(50) NOT NULL
) ENGINE=InnoDB;

CREATE TABLE pagamento (
    id_pagamento INT AUTO_INCREMENT PRIMARY KEY,
    id_locacao INT NOT NULL,
    id_forma_pagamento INT NOT NULL,
    valor DECIMAL(10,2),
    data_pagamento DATETIME,
    status VARCHAR(20),
    FOREIGN KEY (id_locacao) REFERENCES locacao(id_locacao),
    FOREIGN KEY (id_forma_pagamento) REFERENCES forma_pagamento(id_forma_pagamento)
) ENGINE=InnoDB;

CREATE TABLE caucao (
    id_caucao INT AUTO_INCREMENT PRIMARY KEY,
    id_locacao INT NOT NULL,
    valor DECIMAL(10,2),
    status VARCHAR(20),
    FOREIGN KEY (id_locacao) REFERENCES locacao(id_locacao)
) ENGINE=InnoDB;

-- =====================================================
-- MULTA / DANO / VISTORIA
-- =====================================================

CREATE TABLE multa (
    id_multa INT AUTO_INCREMENT PRIMARY KEY,
    id_locacao INT NOT NULL,
    tipo VARCHAR(45),
    valor DECIMAL(10,2),
    status VARCHAR(20),
    FOREIGN KEY (id_locacao) REFERENCES locacao(id_locacao)
) ENGINE=InnoDB;

CREATE TABLE dano (
    id_dano INT AUTO_INCREMENT PRIMARY KEY,
    id_locacao INT NOT NULL,
    descricao TEXT NOT NULL,
    valor DECIMAL(10,2),
    data_registro DATETIME,
    status VARCHAR(20),
    FOREIGN KEY (id_locacao) REFERENCES locacao(id_locacao)
) ENGINE=InnoDB;

CREATE TABLE vistoria (
    id_vistoria INT AUTO_INCREMENT PRIMARY KEY,
    id_locacao INT NOT NULL,
    tipo VARCHAR(45),
    observacao VARCHAR(255),
    data_vistoria DATETIME,
    id_funcionario INT NOT NULL,
    FOREIGN KEY (id_locacao) REFERENCES locacao(id_locacao),
    FOREIGN KEY (id_funcionario) REFERENCES funcionario(id_funcionario)
) ENGINE=InnoDB;

-- =====================================================
-- ADICIONAIS / SEGURO
-- =====================================================

CREATE TABLE adicional (
    id_adicional INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(50),
    valor_diaria DECIMAL(10,2)
) ENGINE=InnoDB;

CREATE TABLE locacao_adicional (
    id_locacao INT NOT NULL,
    id_adicional INT NOT NULL,
    quantidade INT DEFAULT 1,
    PRIMARY KEY (id_locacao, id_adicional),
    FOREIGN KEY (id_locacao) REFERENCES locacao(id_locacao),
    FOREIGN KEY (id_adicional) REFERENCES adicional(id_adicional)
) ENGINE=InnoDB;

CREATE TABLE seguro (
    id_seguro INT AUTO_INCREMENT PRIMARY KEY,
    descricao VARCHAR(100),
    valor_diaria DECIMAL(10,2),
    cobertura TEXT
) ENGINE=InnoDB;

CREATE TABLE locacao_seguro (
    id_locacao INT NOT NULL,
    id_seguro INT NOT NULL,
    PRIMARY KEY (id_locacao, id_seguro),
    FOREIGN KEY (id_locacao) REFERENCES locacao(id_locacao),
    FOREIGN KEY (id_seguro) REFERENCES seguro(id_seguro)
) ENGINE=InnoDB;

-- =====================================================
-- HISTÓRICO
-- =====================================================

CREATE TABLE historico_status_locacao (
    id INT AUTO_INCREMENT PRIMARY KEY,
    id_locacao INT NOT NULL,
    status VARCHAR(20),
    data_status DATETIME,
    id_funcionario INT,
    FOREIGN KEY (id_locacao) REFERENCES locacao(id_locacao),
    FOREIGN KEY (id_funcionario) REFERENCES funcionario(id_funcionario)
) ENGINE=InnoDB;

CREATE TABLE reserva (
    id_reserva INT AUTO_INCREMENT PRIMARY KEY,
    id_cliente INT NOT NULL,
    id_categoria INT NOT NULL,
    data_inicio DATETIME NOT NULL,
    data_fim DATETIME NOT NULL,
    status VARCHAR(20),
    FOREIGN KEY (id_cliente) REFERENCES cliente(id_cliente),
    FOREIGN KEY (id_categoria) REFERENCES categoria_veiculo(id_categoria)
);

CREATE TABLE foto (
    id_foto INT AUTO_INCREMENT PRIMARY KEY,
    entidade VARCHAR(30) NOT NULL, -- VEICULO, VISTORIA, DANO
    id_entidade INT NOT NULL,
    nome_arquivo VARCHAR(255),
    caminho VARCHAR(500),
    data_upload DATETIME DEFAULT CURRENT_TIMESTAMP
);


SET FOREIGN_KEY_CHECKS = 1;
