set -e

if [ $# -ne 1 ]; then
  echo "bash zip.sh (filename)"
  exit 1
fi

rm -rf $1.aky $1.zip
zip -r $1.zip -r $1/ -x "*.DS_Store"
mv $1.zip $1.aky
