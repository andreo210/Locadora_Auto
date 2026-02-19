-- MySQL Workbench Forward Engineering

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';

-- -----------------------------------------------------
-- Schema mydb
-- -----------------------------------------------------
-- -----------------------------------------------------
-- Schema Locadora_autos
-- -----------------------------------------------------

-- -----------------------------------------------------
-- Schema Locadora_autos
-- -----------------------------------------------------
CREATE SCHEMA IF NOT EXISTS `Locadora_autos` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci ;
USE `Locadora_autos` ;

-- -----------------------------------------------------
-- Table `Locadora_autos`.`AspNetRoles`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`AspNetRoles` (
  `Id` VARCHAR(255) NOT NULL,
  `Name` VARCHAR(256) NULL DEFAULT NULL,
  `NormalizedName` VARCHAR(256) NULL DEFAULT NULL,
  `ConcurrencyStamp` LONGTEXT NULL DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE INDEX `RoleNameIndex` (`NormalizedName` ASC) VISIBLE)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`AspNetRoleClaims`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`AspNetRoleClaims` (
  `Id` INT(11) NOT NULL AUTO_INCREMENT,
  `RoleId` VARCHAR(255) NOT NULL,
  `ClaimType` VARCHAR(256) NULL DEFAULT NULL,
  `ClaimValue` VARCHAR(256) NULL DEFAULT NULL,
  PRIMARY KEY (`Id`),
  INDEX `RoleId` (`RoleId` ASC) VISIBLE,
  CONSTRAINT `AspNetRoleClaims_ibfk_1`
    FOREIGN KEY (`RoleId`)
    REFERENCES `Locadora_autos`.`AspNetRoles` (`Id`)
    ON DELETE CASCADE)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`AspNetUserClaims`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`AspNetUserClaims` (
  `Id` INT(11) NOT NULL AUTO_INCREMENT,
  `UserId` VARCHAR(255) NOT NULL,
  `ClaimType` VARCHAR(256) NULL DEFAULT NULL,
  `ClaimValue` VARCHAR(256) NULL DEFAULT NULL,
  PRIMARY KEY (`Id`),
  INDEX `UserId` (`UserId` ASC) VISIBLE,
  CONSTRAINT `AspNetUserClaims_ibfk_1`
    FOREIGN KEY (`UserId`)
    REFERENCES `Locadora_autos`.`aspnet_users` (`id`)
    ON DELETE CASCADE)
ENGINE = InnoDB
AUTO_INCREMENT = 3
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`AspNetUserLogins`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`AspNetUserLogins` (
  `LoginProvider` VARCHAR(255) NOT NULL,
  `ProviderKey` VARCHAR(255) NOT NULL,
  `ProviderDisplayName` VARCHAR(255) NULL DEFAULT NULL,
  `UserId` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`LoginProvider`, `ProviderKey`),
  INDEX `UserId` (`UserId` ASC) VISIBLE,
  CONSTRAINT `AspNetUserLogins_ibfk_1`
    FOREIGN KEY (`UserId`)
    REFERENCES `Locadora_autos`.`aspnet_users` (`id`)
    ON DELETE CASCADE)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`AspNetUserRoles`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`AspNetUserRoles` (
  `UserId` VARCHAR(255) NOT NULL,
  `RoleId` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`UserId`, `RoleId`),
  INDEX `FK_UserRoles_Role` (`RoleId` ASC) VISIBLE,
  CONSTRAINT `FK_UserRoles_Role`
    FOREIGN KEY (`RoleId`)
    REFERENCES `Locadora_autos`.`AspNetRoles` (`Id`)
    ON DELETE CASCADE)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`AspNetUsers`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`AspNetUsers` (
  `Id` VARCHAR(255) NOT NULL,
  `UserName` VARCHAR(256) NULL DEFAULT NULL,
  `NormalizedUserName` VARCHAR(256) NULL DEFAULT NULL,
  `Email` VARCHAR(256) NULL DEFAULT NULL,
  `NormalizedEmail` VARCHAR(256) NULL DEFAULT NULL,
  `EmailConfirmed` BIT(1) NOT NULL DEFAULT b'0',
  `PasswordHash` LONGTEXT NULL DEFAULT NULL,
  `SecurityStamp` LONGTEXT NULL DEFAULT NULL,
  `ConcurrencyStamp` VARCHAR(36) NULL DEFAULT NULL,
  `PhoneNumber` VARCHAR(50) NULL DEFAULT NULL,
  `PhoneNumberConfirmed` BIT(1) NOT NULL DEFAULT b'0',
  `TwoFactorEnabled` BIT(1) NOT NULL DEFAULT b'0',
  `LockoutEnd` DATETIME(6) NULL DEFAULT NULL,
  `LockoutEnabled` BIT(1) NOT NULL DEFAULT b'0',
  `AccessFailedCount` INT(11) NOT NULL DEFAULT 0,
  `NomeCompleto` VARCHAR(255) NULL DEFAULT NULL,
  `Ativo` BIT(1) NOT NULL DEFAULT b'1',
  `DataCriacao` DATETIME NULL DEFAULT CURRENT_TIMESTAMP(),
  `Cpf` VARCHAR(45) NULL DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE INDEX `NormalizedUserName` (`NormalizedUserName` ASC) VISIBLE)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`refreshTokens`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`refreshTokens` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `token` MEDIUMTEXT NOT NULL,
  `expira_em` DATETIME NOT NULL,
  `revogado` BIT(1) NULL DEFAULT b'0',
  `criado_em` DATETIME NULL DEFAULT CURRENT_TIMESTAMP(),
  `user_id` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE INDEX `token` USING HASH (`token`) VISIBLE,
  INDEX `iduserFk_idx` (`user_id` ASC) VISIBLE,
  CONSTRAINT `iduserFk`
    FOREIGN KEY (`user_id`)
    REFERENCES `Locadora_autos`.`AspNetUsers` (`Id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB
AUTO_INCREMENT = 35
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbAdicional`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbAdicional` (
  `id_adicional` INT(11) NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(50) NULL DEFAULT NULL,
  `valor_diaria` DECIMAL(10,2) NULL DEFAULT NULL,
  `ativo` BIT(1) NULL DEFAULT NULL,
  PRIMARY KEY (`id_adicional`))
ENGINE = InnoDB
AUTO_INCREMENT = 2
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbCategoria_veiculo`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbCategoria_veiculo` (
  `id_categoria` INT(11) NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(50) NOT NULL,
  `valor_diaria` DECIMAL(10,2) NOT NULL,
  `limite_km` INT(11) NULL DEFAULT NULL,
  `valor_km_excedente` DECIMAL(10,2) NULL DEFAULT NULL,
  PRIMARY KEY (`id_categoria`))
ENGINE = InnoDB
AUTO_INCREMENT = 7
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbCliente`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbCliente` (
  `id_cliente` INT(11) NOT NULL AUTO_INCREMENT,
  `ativo` BIT(1) NULL DEFAULT b'1',
  `numero_habilitacao` VARCHAR(45) NULL DEFAULT NULL,
  `validade_habilitacao` VARCHAR(45) NULL DEFAULT NULL,
  `total_locacoes` INT(11) NULL DEFAULT NULL,
  `data_criacao` DATETIME NULL DEFAULT NULL,
  `data_modificacao` DATETIME NULL DEFAULT NULL,
  `id_usuario_criacao` VARCHAR(100) NULL DEFAULT NULL,
  `id_usuario_modificacao` VARCHAR(100) NULL DEFAULT NULL,
  `idAspNetUsers` VARCHAR(255) NOT NULL,
  `status` VARCHAR(45) NULL DEFAULT NULL,
  PRIMARY KEY (`id_cliente`),
  INDEX `fkCliente_User_idx` (`idAspNetUsers` ASC) VISIBLE,
  CONSTRAINT `fkCliente_User`
    FOREIGN KEY (`idAspNetUsers`)
    REFERENCES `Locadora_autos`.`AspNetUsers` (`Id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB
AUTO_INCREMENT = 13
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbEndereco`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbEndereco` (
  `id_endereco` INT(11) NOT NULL AUTO_INCREMENT,
  `id_cliente` INT(11) NULL DEFAULT NULL,
  `logradouro` VARCHAR(150) NULL DEFAULT NULL,
  `numero` VARCHAR(20) NULL DEFAULT NULL,
  `complemento` VARCHAR(100) NULL DEFAULT NULL,
  `bairro` VARCHAR(100) NULL DEFAULT NULL,
  `cidade` VARCHAR(100) NULL DEFAULT NULL,
  `estado` CHAR(2) NULL DEFAULT NULL,
  `cep` CHAR(8) NULL DEFAULT NULL,
  PRIMARY KEY (`id_endereco`),
  INDEX `fk_endereco_cliente_idx` (`id_cliente` ASC) VISIBLE,
  CONSTRAINT `fk_endereco_cliente`
    FOREIGN KEY (`id_cliente`)
    REFERENCES `Locadora_autos`.`tbCliente` (`id_cliente`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB
AUTO_INCREMENT = 41
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbFilial`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbFilial` (
  `id_filial` INT(11) NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(100) NOT NULL,
  `cidade` VARCHAR(100) NOT NULL,
  `ativo` BIT(1) NULL DEFAULT b'1',
  `idEndereco` INT(11) NOT NULL,
  PRIMARY KEY (`id_filial`),
  INDEX `fkEndereco_idx` (`idEndereco` ASC) VISIBLE,
  CONSTRAINT `fkEndereco`
    FOREIGN KEY (`idEndereco`)
    REFERENCES `Locadora_autos`.`tbEndereco` (`id_endereco`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB
AUTO_INCREMENT = 9
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbVeiculo`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbVeiculo` (
  `id_veiculo` INT(11) NOT NULL AUTO_INCREMENT,
  `placa` VARCHAR(10) NOT NULL,
  `chassi` VARCHAR(30) NOT NULL,
  `id_categoria` INT(11) NOT NULL,
  `km_atual` INT(11) NOT NULL,
  `id_filial_atual` INT(11) NOT NULL,
  `ativo` BIT(1) NULL DEFAULT b'1',
  `marca` VARCHAR(45) NULL DEFAULT NULL,
  `modelo` VARCHAR(45) NULL DEFAULT NULL,
  `ano` INT(11) NULL DEFAULT NULL,
  `disponivel` BIT(1) NULL DEFAULT b'1',
  `status` INT(11) NULL DEFAULT NULL,
  PRIMARY KEY (`id_veiculo`),
  UNIQUE INDEX `placa` (`placa` ASC) VISIBLE,
  UNIQUE INDEX `chassi` (`chassi` ASC) VISIBLE,
  INDEX `id_categoria` (`id_categoria` ASC) VISIBLE,
  INDEX `id_filial_atual` (`id_filial_atual` ASC) VISIBLE,
  CONSTRAINT `tbVeiculo_ibfk_1`
    FOREIGN KEY (`id_categoria`)
    REFERENCES `Locadora_autos`.`tbCategoria_veiculo` (`id_categoria`),
  CONSTRAINT `tbVeiculo_ibfk_2`
    FOREIGN KEY (`id_filial_atual`)
    REFERENCES `Locadora_autos`.`tbFilial` (`id_filial`))
ENGINE = InnoDB
AUTO_INCREMENT = 10
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbFuncionario`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbFuncionario` (
  `id_funcionario` INT(11) NOT NULL AUTO_INCREMENT,
  `matricula` VARCHAR(20) NOT NULL,
  `cargo` VARCHAR(50) NULL DEFAULT NULL,
  `status` BIT(1) NULL DEFAULT b'1',
  `id_user` VARCHAR(100) NOT NULL,
  PRIMARY KEY (`id_funcionario`),
  UNIQUE INDEX `matricula` (`matricula` ASC) VISIBLE,
  INDEX `fk_user_funcionario_idx` (`id_user` ASC) VISIBLE,
  CONSTRAINT `fk_user_funcionario`
    FOREIGN KEY (`id_user`)
    REFERENCES `Locadora_autos`.`AspNetUsers` (`Id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB
AUTO_INCREMENT = 7
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbLocacao`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbLocacao` (
  `id_locacao` INT(11) NOT NULL AUTO_INCREMENT,
  `id_cliente` INT(11) NOT NULL,
  `id_veiculo` INT(11) NOT NULL,
  `id_funcionario` INT(11) NOT NULL,
  `id_filial_retirada` INT(11) NOT NULL,
  `id_filial_devolucao` INT(11) NULL DEFAULT NULL,
  `data_inicio` DATETIME NOT NULL,
  `data_fim_prevista` DATETIME NOT NULL,
  `data_fim_real` DATETIME NULL DEFAULT NULL,
  `km_inicial` INT(11) NOT NULL,
  `km_final` INT(11) NULL DEFAULT NULL,
  `valor_previsto` DECIMAL(10,2) NOT NULL,
  `valor_final` DECIMAL(10,2) NULL DEFAULT NULL,
  `status` VARCHAR(20) NOT NULL,
  `id_reserva` VARCHAR(45) NULL DEFAULT NULL,
  PRIMARY KEY (`id_locacao`),
  INDEX `id_veiculo` (`id_veiculo` ASC) VISIBLE,
  INDEX `id_funcionario` (`id_funcionario` ASC) VISIBLE,
  INDEX `id_filial_retirada` (`id_filial_retirada` ASC) VISIBLE,
  INDEX `id_filial_devolucao` (`id_filial_devolucao` ASC) VISIBLE,
  INDEX `locacao_fk_cliente_idx` (`id_cliente` ASC) VISIBLE,
  CONSTRAINT `locacao_fk_cliente`
    FOREIGN KEY (`id_cliente`)
    REFERENCES `Locadora_autos`.`tbCliente` (`id_cliente`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `tbLocacao_ibfk_2`
    FOREIGN KEY (`id_veiculo`)
    REFERENCES `Locadora_autos`.`tbVeiculo` (`id_veiculo`),
  CONSTRAINT `tbLocacao_ibfk_3`
    FOREIGN KEY (`id_funcionario`)
    REFERENCES `Locadora_autos`.`tbFuncionario` (`id_funcionario`),
  CONSTRAINT `tbLocacao_ibfk_4`
    FOREIGN KEY (`id_filial_retirada`)
    REFERENCES `Locadora_autos`.`tbFilial` (`id_filial`),
  CONSTRAINT `tbLocacao_ibfk_5`
    FOREIGN KEY (`id_filial_devolucao`)
    REFERENCES `Locadora_autos`.`tbFilial` (`id_filial`))
ENGINE = InnoDB
AUTO_INCREMENT = 6
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbCaucao`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbCaucao` (
  `id_caucao` INT(11) NOT NULL AUTO_INCREMENT,
  `id_locacao` INT(11) NOT NULL,
  `valor` DECIMAL(10,2) NULL DEFAULT NULL,
  `status` VARCHAR(20) NULL DEFAULT NULL,
  PRIMARY KEY (`id_caucao`),
  INDEX `id_locacao` (`id_locacao` ASC) VISIBLE,
  CONSTRAINT `tbCaucao_ibfk_1`
    FOREIGN KEY (`id_locacao`)
    REFERENCES `Locadora_autos`.`tbLocacao` (`id_locacao`))
ENGINE = InnoDB
AUTO_INCREMENT = 2
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbClienteHistorico`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbClienteHistorico` (
  `idHistorico` INT(11) NOT NULL AUTO_INCREMENT,
  `id_cliente` INT(11) NULL DEFAULT NULL,
  `status` BIT(1) NULL DEFAULT b'1',
  `numero_habilitacao` VARCHAR(45) NULL DEFAULT NULL,
  `validade_habilitacao` DATETIME NULL DEFAULT NULL,
  `total_locacoes` INT(11) NULL DEFAULT NULL,
  `data_evento` DATETIME NULL DEFAULT NULL,
  `acao` VARCHAR(10) NULL DEFAULT NULL,
  `usuario_evento` VARCHAR(45) NULL DEFAULT NULL,
  `Id_usuario_modificacao` VARCHAR(100) NULL DEFAULT NULL,
  PRIMARY KEY (`idHistorico`))
ENGINE = InnoDB
AUTO_INCREMENT = 11
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbVistoria`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbVistoria` (
  `id_vistoria` INT(11) NOT NULL AUTO_INCREMENT,
  `id_locacao` INT(11) NOT NULL,
  `tipo` INT(2) NULL DEFAULT NULL,
  `observacoes` VARCHAR(255) NULL DEFAULT NULL,
  `data_vistoria` DATETIME NULL DEFAULT NULL,
  `id_funcionario` INT(11) NOT NULL,
  `nivel_combustivel` INT(2) NULL DEFAULT NULL,
  `km_veiculo` INT(13) NULL DEFAULT NULL,
  PRIMARY KEY (`id_vistoria`),
  INDEX `id_locacao` (`id_locacao` ASC) VISIBLE,
  INDEX `id_funcionario` (`id_funcionario` ASC) VISIBLE,
  CONSTRAINT `tbVistoria_ibfk_1`
    FOREIGN KEY (`id_locacao`)
    REFERENCES `Locadora_autos`.`tbLocacao` (`id_locacao`),
  CONSTRAINT `tbVistoria_ibfk_2`
    FOREIGN KEY (`id_funcionario`)
    REFERENCES `Locadora_autos`.`tbFuncionario` (`id_funcionario`))
ENGINE = InnoDB
AUTO_INCREMENT = 5
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbDano`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbDano` (
  `id_dano` INT(11) NOT NULL AUTO_INCREMENT,
  `id_vistoria` INT(11) NOT NULL,
  `tipo_dano` INT(11) NOT NULL,
  `valor_estimado` DECIMAL(10,2) NULL DEFAULT NULL,
  `data_registro` DATETIME NULL DEFAULT NULL,
  `tipo_status` INT(11) NULL DEFAULT NULL,
  `descricao` VARCHAR(500) NULL DEFAULT NULL,
  PRIMARY KEY (`id_dano`),
  INDEX `tbDano_ibfk_1_idx` (`id_vistoria` ASC) VISIBLE,
  CONSTRAINT `tbDano_ibfk_1`
    FOREIGN KEY (`id_vistoria`)
    REFERENCES `Locadora_autos`.`tbVistoria` (`id_vistoria`))
ENGINE = InnoDB
AUTO_INCREMENT = 2
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbForma_pagamento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbForma_pagamento` (
  `id_forma_pagamento` INT(11) NOT NULL AUTO_INCREMENT,
  `descricao` VARCHAR(50) NOT NULL,
  PRIMARY KEY (`id_forma_pagamento`))
ENGINE = InnoDB
AUTO_INCREMENT = 5
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbFotoCategoriaVeiculo`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbFotoCategoriaVeiculo` (
  `id_foto` INT(11) NOT NULL AUTO_INCREMENT,
  `id_categoria_veiculo` INT(11) NULL DEFAULT NULL,
  `nome_arquivo` VARCHAR(255) NULL DEFAULT NULL,
  `raiz` VARCHAR(255) NULL DEFAULT NULL,
  `quantidadeBytes` BIGINT(45) NULL DEFAULT NULL,
  `data_upload` DATETIME NULL DEFAULT NULL,
  `diretorio` VARCHAR(45) NULL DEFAULT NULL,
  `extensao` VARCHAR(45) NULL DEFAULT NULL,
  PRIMARY KEY (`id_foto`),
  INDEX `fk_categoria_idx` (`id_categoria_veiculo` ASC) VISIBLE,
  CONSTRAINT `fk_categoria`
    FOREIGN KEY (`id_categoria_veiculo`)
    REFERENCES `Locadora_autos`.`tbCategoria_veiculo` (`id_categoria`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB
AUTO_INCREMENT = 2
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbFotoFilial`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbFotoFilial` (
  `id_foto` INT(11) NOT NULL AUTO_INCREMENT,
  `id_filial` INT(11) NOT NULL,
  `nome_arquivo` VARCHAR(255) NULL DEFAULT NULL,
  `raiz` VARCHAR(255) NULL DEFAULT NULL,
  `data_upload` DATETIME NULL DEFAULT NULL,
  `diretorio` VARCHAR(45) NULL DEFAULT NULL,
  `extensao` VARCHAR(45) NULL DEFAULT NULL,
  `tbFotoFilialcol` VARCHAR(45) NULL DEFAULT NULL,
  `quantidadeBytes` BIGINT(50) NULL DEFAULT NULL,
  PRIMARY KEY (`id_foto`),
  INDEX `fkFilial_idx` (`id_filial` ASC) VISIBLE,
  CONSTRAINT `fk_filiak`
    FOREIGN KEY (`id_filial`)
    REFERENCES `Locadora_autos`.`tbFilial` (`id_filial`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB
AUTO_INCREMENT = 2
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbFotoVistoria`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbFotoVistoria` (
  `id_foto` INT(11) NOT NULL AUTO_INCREMENT,
  `id_vistoria` INT(11) NOT NULL,
  `nome_arquivo` VARCHAR(255) NULL DEFAULT NULL,
  `raiz` VARCHAR(500) NULL DEFAULT NULL,
  `data_upload` DATETIME NULL DEFAULT CURRENT_TIMESTAMP(),
  `diretorio` VARCHAR(45) NULL DEFAULT NULL,
  `extensao` VARCHAR(45) NULL DEFAULT NULL,
  `quantidadeBytes` BIGINT(50) NULL DEFAULT NULL,
  PRIMARY KEY (`id_foto`),
  INDEX `fkVistoria_idx` (`id_vistoria` ASC) VISIBLE,
  CONSTRAINT `fkVistoria`
    FOREIGN KEY (`id_vistoria`)
    REFERENCES `Locadora_autos`.`tbVistoria` (`id_vistoria`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB
AUTO_INCREMENT = 12
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbLocacaoAdicional`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbLocacaoAdicional` (
  `id_locacao` INT(11) NOT NULL,
  `id_adicional` INT(11) NOT NULL,
  `quantidade` INT(11) NULL DEFAULT 1,
  `dias` INT(11) NULL DEFAULT NULL,
  `valor_diaria` DECIMAL(10,2) NULL DEFAULT NULL,
  `id_locacao_adicional` INT(11) NOT NULL AUTO_INCREMENT,
  `valor_total` DECIMAL(10,2) NULL DEFAULT NULL,
  PRIMARY KEY (`id_locacao_adicional`),
  INDEX `id_adicional` (`id_adicional` ASC) VISIBLE,
  INDEX `tbLocacaoAdicional_ibfk_1` (`id_locacao` ASC) VISIBLE,
  CONSTRAINT `tbLocacaoAdicional_ibfk_1`
    FOREIGN KEY (`id_locacao`)
    REFERENCES `Locadora_autos`.`tbLocacao` (`id_locacao`),
  CONSTRAINT `tbLocacaoAdicional_ibfk_2`
    FOREIGN KEY (`id_adicional`)
    REFERENCES `Locadora_autos`.`tbAdicional` (`id_adicional`))
ENGINE = InnoDB
AUTO_INCREMENT = 4
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbSeguro`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbSeguro` (
  `id_seguro` INT(11) NOT NULL AUTO_INCREMENT,
  `descricao` VARCHAR(100) NULL DEFAULT NULL,
  `valor_diaria` DECIMAL(10,2) NULL DEFAULT NULL,
  `cobertura` TEXT NULL DEFAULT NULL,
  `nome` VARCHAR(45) NULL DEFAULT NULL,
  `ativo` BIT(1) NULL DEFAULT b'1',
  `franquia` DECIMAL(10,2) NULL DEFAULT NULL,
  PRIMARY KEY (`id_seguro`))
ENGINE = InnoDB
AUTO_INCREMENT = 2
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbLocacao_seguro`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbLocacao_seguro` (
  `id_locacao` INT(11) NOT NULL,
  `id_seguro` INT(11) NOT NULL,
  `id_locacao_seguro` INT(11) NOT NULL AUTO_INCREMENT,
  `ativo` BIT(1) NOT NULL,
  PRIMARY KEY (`id_locacao_seguro`),
  INDEX `FK_locacao_idx` (`id_locacao` ASC) VISIBLE,
  INDEX `FK_seguros` (`id_seguro` ASC) VISIBLE,
  CONSTRAINT `tbLocacao_seguro_ibfk_1`
    FOREIGN KEY (`id_locacao`)
    REFERENCES `Locadora_autos`.`tbLocacao` (`id_locacao`),
  CONSTRAINT `tbLocacao_seguro_ibfk_2`
    FOREIGN KEY (`id_seguro`)
    REFERENCES `Locadora_autos`.`tbSeguro` (`id_seguro`))
ENGINE = InnoDB
AUTO_INCREMENT = 3
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbManutencao`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbManutencao` (
  `id_manutencao` INT(11) NOT NULL AUTO_INCREMENT,
  `id_veiculo` INT(11) NOT NULL,
  `tipo_manutencao` INT(11) NULL DEFAULT NULL,
  `descricao` TEXT NULL DEFAULT NULL,
  `custo` DECIMAL(10,2) NULL DEFAULT NULL,
  `data_inicio` DATETIME NULL DEFAULT NULL,
  `data_fim` DATETIME NULL DEFAULT NULL,
  `status_manutencao` INT(11) NULL DEFAULT NULL,
  PRIMARY KEY (`id_manutencao`),
  INDEX `id_veiculo` (`id_veiculo` ASC) VISIBLE,
  CONSTRAINT `tbManutencao_ibfk_1`
    FOREIGN KEY (`id_veiculo`)
    REFERENCES `Locadora_autos`.`tbVeiculo` (`id_veiculo`))
ENGINE = InnoDB
AUTO_INCREMENT = 2
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbMulta`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbMulta` (
  `id_multa` INT(11) NOT NULL AUTO_INCREMENT,
  `id_locacao` INT(11) NOT NULL,
  `tipo` VARCHAR(45) NULL DEFAULT NULL,
  `valor` DECIMAL(10,2) NULL DEFAULT NULL,
  `status` VARCHAR(20) NULL DEFAULT NULL,
  PRIMARY KEY (`id_multa`),
  INDEX `id_locacao` (`id_locacao` ASC) VISIBLE,
  CONSTRAINT `tbMulta_ibfk_1`
    FOREIGN KEY (`id_locacao`)
    REFERENCES `Locadora_autos`.`tbLocacao` (`id_locacao`))
ENGINE = InnoDB
AUTO_INCREMENT = 3
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbPagamento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbPagamento` (
  `id_pagamento` INT(11) NOT NULL AUTO_INCREMENT,
  `id_locacao` INT(11) NOT NULL,
  `id_forma_pagamento` INT(11) NOT NULL,
  `valor` DECIMAL(10,2) NULL DEFAULT NULL,
  `data_pagamento` DATETIME NULL DEFAULT NULL,
  `status` VARCHAR(20) NULL DEFAULT NULL,
  PRIMARY KEY (`id_pagamento`),
  INDEX `id_locacao` (`id_locacao` ASC) VISIBLE,
  INDEX `id_forma_pagamento` (`id_forma_pagamento` ASC) VISIBLE,
  CONSTRAINT `tbPagamento_ibfk_1`
    FOREIGN KEY (`id_locacao`)
    REFERENCES `Locadora_autos`.`tbLocacao` (`id_locacao`),
  CONSTRAINT `tbPagamento_ibfk_2`
    FOREIGN KEY (`id_forma_pagamento`)
    REFERENCES `Locadora_autos`.`tbForma_pagamento` (`id_forma_pagamento`))
ENGINE = InnoDB
AUTO_INCREMENT = 2
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbReserva`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbReserva` (
  `id_reserva` INT(11) NOT NULL AUTO_INCREMENT,
  `id_cliente` INT(11) NOT NULL,
  `id_categoria_veiculo` INT(11) NOT NULL,
  `data_inicio` DATETIME NOT NULL,
  `data_fim` DATETIME NOT NULL,
  `status` INT(11) NULL DEFAULT NULL,
  `ativo` BIT(1) NULL DEFAULT NULL,
  `id_filial` INT(11) NOT NULL,
  PRIMARY KEY (`id_reserva`),
  INDEX `FKCliente_idx` (`id_cliente` ASC) VISIBLE,
  INDEX `FKVeiculo_idx` (`id_categoria_veiculo` ASC) VISIBLE,
  INDEX `FKFilial_idx` (`id_filial` ASC) VISIBLE,
  CONSTRAINT `FKCliente`
    FOREIGN KEY (`id_cliente`)
    REFERENCES `Locadora_autos`.`tbCliente` (`id_cliente`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `FKFilial`
    FOREIGN KEY (`id_filial`)
    REFERENCES `Locadora_autos`.`tbFilial` (`id_filial`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `FKVeiculo`
    FOREIGN KEY (`id_categoria_veiculo`)
    REFERENCES `Locadora_autos`.`tbCategoria_veiculo` (`id_categoria`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB
AUTO_INCREMENT = 9
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


-- -----------------------------------------------------
-- Table `Locadora_autos`.`tbUserHistorico`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Locadora_autos`.`tbUserHistorico` (
  `id_historico` INT(11) NOT NULL AUTO_INCREMENT,
  `id` VARCHAR(145) NULL DEFAULT NULL,
  `nome_completo` VARCHAR(300) NULL DEFAULT NULL,
  `phone_number` VARCHAR(45) NULL DEFAULT NULL,
  `email` VARCHAR(45) NULL DEFAULT NULL,
  `data_evento` DATETIME NULL DEFAULT NULL,
  `acao` VARCHAR(15) NULL DEFAULT NULL,
  `usuario_evento` VARCHAR(145) NULL DEFAULT NULL,
  PRIMARY KEY (`id_historico`))
ENGINE = InnoDB
AUTO_INCREMENT = 27
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_unicode_ci;


SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
