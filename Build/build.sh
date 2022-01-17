# This batch file is used to generate the final "Unity Editor Integration Package" for the "UnityScript2CSharp" converter
#
# Requirements:
#	- gzip must be in the search path
# 	- msbuild must be in the search path
# 	- Unity must be in the searh path 

expand_bg="\e[K"
blue_bg="\e[0;104m${expand_bg}"
red_bg="\e[0;101m${expand_bg}"
green_bg="\e[0;102m${expand_bg}"

red="\e[0;91m"
blue="\e[0;94m"
green="\e[0;92m"
white="\e[0;97m"
bold="\e[1m"
uline="\e[4m"
reset="\e[0m"

function CheckExecutable()
{
   which $1 > /dev/null
   
   if [ $? -ne 0 ]; then
      echo Could not find $1. Please make sure it is in your path and try again.
      exit
   fi
}

CheckExecutable msbuild
if [ $? -ne 0 ]; then
   DependenciesNotFound
fi 

CheckExecutable Unity
if [ $? -ne 0 ]; then
   DependenciesNotFound
fi 

# Create Project Folder
UNITYSCRIPT2CSHARP_PROJECT_ROOT=/tmp/UnityScript2CSharpBuildProject
if [ -d $UNITYSCRIPT2CSHARP_PROJECT_ROOT ]; then
   rm -rf $UNITYSCRIPT2CSHARP_PROJECT_ROOT
fi
mkdir $UNITYSCRIPT2CSHARP_PROJECT_ROOT

# Build app
msbuild /p:Configuration=Release

if [ $? -ne 0 ]; then
   echo Error while building converter. Error code = $?
   exit
fi

# Extracts version # from output like: UnityScript2CSharp 1.0.6577.20371
CONVERTER_VERSION="$(mono UnityScript2CSharp/bin/Release/UnityScript2CSharp.exe 2>&1 | head -n 1 | cut -b20-40)"
echo $CONVERTER_VERSION

# Create folders
UNITYSCRIPT2CSHARP_IN_ASSETS_FOLDER=$UNITYSCRIPT2CSHARP_PROJECT_ROOT/Assets/UnityScript2CSharp
mkdir -p $UNITYSCRIPT2CSHARP_IN_ASSETS_FOLDER

# Zip output
OUTPUT_FILE="$UNITYSCRIPT2CSHARP_IN_ASSETS_FOLDER/UnityScript2CSharp_$CONVERTER_VERSION.zip"

echo Generating zip file: $OUTPUT_FILE

UNITY_APP_PATH="$(which Unity)"
UNITY_INSTALL_FOLDER="${UNITY_APP_PATH%/Unity}"

pushd UnityScript2CSharp/bin/Release/
$UNITY_INSTALL_FOLDER/Data/Tools/7za a -tzip $OUTPUT_FILE *
COMPRESSING_STATUS=$?
popd

if [ $COMPRESSING_STATUS -ne 0 ]; then
   echo -e "${red}Failed to generate zip file. Aborting${reset}"
   exit
fi

# Copy editor integration sources..
mkdir $UNITYSCRIPT2CSHARP_IN_ASSETS_FOLDER/Editor
cp EditorIntegration/Assets/UnityScript2CSharp/Editor/* $UNITYSCRIPT2CSHARP_IN_ASSETS_FOLDER/Editor/

# Create package

EXPORTED_PACKAGE_PATH="/tmp/UnityScript2CSharp_Conversion_$CONVERTER_VERSION.unitypackage"
echo -e "${green_bg}${white}Exporting unity package from project $UNITYSCRIPT2CSHARP_PROJECT_ROOT to ${EXPORTED_PACKAGE_PATH}${reset}"

Unity --createProject -batchmode -projectPath $UNITYSCRIPT2CSHARP_PROJECT_ROOT -exportPackage Assets $EXPORTED_PACKAGE_PATH -quit

if [ $? -ne 0 ]; then 
   echo Error while exporting Unity package. Error code = $?
   exit
fi

echo -e "${white}Unity package exported successfully${reset}"