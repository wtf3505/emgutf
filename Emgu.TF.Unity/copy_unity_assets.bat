cd Assets
cd Emgu.TF
rm -rf Emgu.TF.Util
mkdir Emgu.TF.Util
rm -rf Emgu.TF
mkdir Emgu.TF
rm -rf Emgu.TF.Models
mkdir Emgu.TF.Models
rm -rf Emgu.Models
mkdir Emgu.Models
cd ..
cd ..

cp ../tensorflow/LICENSE Assets/Emgu.TF/tensorflow.license.txt
cp unityStoreIcons/README.txt Assets/Emgu.TF/README.txt

cp ../Emgu.TF.Util/*.cs Assets/Emgu.TF/Emgu.TF.Util/
cp ../Emgu.TF/*.cs Assets/Emgu.TF/Emgu.TF/
cp ../Emgu.TF.Models/*.cs Assets/Emgu.TF/Emgu.TF.Models/
cp ../Emgu.Models/*.cs Assets/Emgu.TF/Emgu.Models/

