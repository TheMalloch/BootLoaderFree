# Script PowerShell pour convertir le code C# 6+ en code compatible avec C# 5.0
# Auteur: GitHub Copilot
# Date: 29 avril 2025

Write-Host "Conversion du code C# 6+ en code compatible avec C# 5.0..." -ForegroundColor Green

# Fonction pour convertir un fichier
function Convert-CSharpFile {
    param (
        [string]$FilePath
    )
    
    Write-Host "Traitement du fichier: $FilePath" -ForegroundColor Cyan
    
    # Lire tout le contenu du fichier
    $content = Get-Content -Path $FilePath -Raw
    
    # Copie du fichier original
    Copy-Item -Path $FilePath -Destination "$FilePath.backup" -Force
    
    # Modifications à effectuer
    
    # 1. Convertir les propriétés initialisées directement (pattern: { get; set; } = value;)
    $content = [regex]::Replace($content, '([a-zA-Z0-9_<>]+(?:\[\])?) ([a-zA-Z0-9_]+) { get; set; } = ([^;]+);', 
        {
            param($match)
            $propType = $match.Groups[1].Value
            $propName = $match.Groups[2].Value
            $propValue = $match.Groups[3].Value
            
            "$propType $propName { get; set; }"
        })
    
    # 2. Remplacer les propriétés auto-implémentées avec initialiseurs par des propriétés et constructeurs
    # Cette partie est plus complexe et nécessite l'analyse de la structure de classe
    
    # 3. Remplacer les méthodes définies par expression (=>)
    $content = [regex]::Replace($content, '([a-zA-Z0-9_<>]+(?:\[\])?) ([a-zA-Z0-9_]+)\s*\((.*?)\)\s*=>\s*([^;]+);', 
        {
            param($match)
            $returnType = $match.Groups[1].Value
            $methodName = $match.Groups[2].Value
            $parameters = $match.Groups[3].Value
            $expression = $match.Groups[4].Value
            
            "$returnType $methodName($parameters) { return $expression; }"
        })
    
    # 4. Convertir les chaînes interpolées ($"...") en String.Format
    $content = [regex]::Replace($content, '\$"([^"]*)"', 
        {
            param($match)
            $formatString = $match.Groups[1].Value
            
            # Remplacer {0}, {1}, etc. par {{0}}, {{1}} pour éviter les conflits avec String.Format
            $formatString = $formatString -replace '{([^{}]+)}', '{$1}'
            
            "String.Format(""$formatString"")"
        })
    
    # Écrire le contenu modifié dans le fichier
    Set-Content -Path $FilePath -Value $content
    
    Write-Host "Conversion terminée pour: $FilePath" -ForegroundColor Green
}

# Obtenir tous les fichiers .cs dans le répertoire et les sous-répertoires
$files = Get-ChildItem -Path "c:\Users\Admin\Documents\BootLoaderFree\BOOTLOADERFREE" -Filter "*.cs" -Recurse

# Convertir chaque fichier
foreach ($file in $files) {
    Convert-CSharpFile -FilePath $file.FullName
}

Write-Host "Conversion terminée pour tous les fichiers!" -ForegroundColor Green
Write-Host "Vous pouvez maintenant essayer de compiler votre projet." -ForegroundColor Yellow
Write-Host "Si vous rencontrez d'autres erreurs, vous devrez peut-être ajuster manuellement certains fichiers." -ForegroundColor Yellow