-- Script SQL pour ajouter des catégories par défaut
-- Exécutez ce script dans SQL Server Management Studio

USE [ProjetTestDotNetDb]
GO

-- Insérer les catégories
INSERT INTO Categories (Nom, Description) VALUES 
('Lighting', 'Lampes, luminaires et éclairage'),
('Kitchenware', 'Ustensiles et articles de cuisine'),
('Home Decor', 'Décoration et accessoires maison'),
('Office', 'Mobilier et fournitures de bureau'),
('Plants', 'Plantes et accessoires de jardinage'),
('Furniture', 'Meubles et ameublement');

-- Vérifier l'insertion
SELECT * FROM Categories;
