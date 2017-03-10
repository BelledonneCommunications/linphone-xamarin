#!/usr/bin/python

# Copyright (C) 2017 Belledonne Communications SARL
# 
# This program is free software; you can redistribute it and/or
# modify it under the terms of the GNU General Public License
# as published by the Free Software Foundation; either version 2
# of the License, or (at your option) any later version.
# 
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
# 
# You should have received a copy of the GNU General Public License
# along with this program; if not, write to the Free Software
# Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

import argparse
import os
import sys
import pystache

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..', 'tools'))
print sys.path
import genapixml as CApi
import abstractapi as AbsApi

class CsharpTranslator(object):
	def __init__(self):
		self.ignore = []

	def translate_method_name(self, name, recursive=False, topAncestor=None):
		translatedName = name.to_camel_case(lower=True)
			
		if name.prev is None or not recursive or name.prev is topAncestor:
			return translatedName

	def translate_argument_name(self, name):
		argname = name.to_camel_case(lower=True)
		if argname == "params":
			return "parameters"
		elif argname == "event":
			return "ev"
		elif argname == "ref":
			return "reference"
		elif argname == "value":
			return "val"
		return argname

	def translate_base_type(self, _type, isArg):
		if _type.name == 'void':
				return 'void'
		elif _type.name == 'boolean':
			res = 'int' # In C the bool_t is an integer
		elif _type.name == 'integer':
			if _type.isUnsigned:
				res = 'uint'
			else:
				res = 'int'
		elif _type.name == 'string':
			if isArg:
				return 'string'
			else:
				res = 'IntPtr' # Return as IntPtr and get string with Marshal.PtrToStringAnsi()
		elif _type.name == 'character':
			if _type.isUnsigned:
				res = 'byte'
			else:
				res = 'sbyte'
		elif _type.name == 'time':
			res = 'long' #TODO check
		elif _type.name == 'size':
			res = 'long' #TODO check
		elif _type.name == 'floatant':
			return 'float'
		elif _type.name == 'string_array':
			return 'string[]'
		else:
			raise AbsApi.Error('\'{0}\' is not a base abstract type'.format(_type.name))

		return res
	
	def translate_type(self, aType, isArg):
		if (aType.isref):
			return "IntPtr"
		elif type(aType) is AbsApi.EnumType:
			return "IntPtr" # ?
		elif type(aType) is AbsApi.ClassType:
			return "IntPtr"
		elif type(aType) is AbsApi.BaseType:
			return self.translate_base_type(aType, isArg)
		elif type(aType) is AbsApi.ListType:
			raise AbsApi.Error('Lists are not supported right now') #TODO
	
	def translate_argument(self, arg):
		return '{0} {1}'.format(self.translate_type(arg.type, True), self.translate_argument_name(arg.name))

	def translate_method(self, method, static=False, genImpl=True):
		if method.name.to_snake_case(fullName=True) in self.ignore:
			raise AbsApi.Error('{0} has been escaped'.format(method.name.to_snake_case(fullName=True)))

		methodElems = {}
		methodElems['return'] = self.translate_type(method.returnType, False)
		methodElems['name'] = method.name.to_c()
		methodElems['params'] = '' if static else 'IntPtr thiz'
		for arg in method.args:
			if arg is not method.args[0] or not static:
				methodElems['params'] += ', '
			methodElems['params'] += self.translate_argument(arg)
		
		methodDict = {}
		methodDict['prototype'] = """[DllImport(LinphoneWrapper.LIB_NAME)]
		static extern {return} {name}({params});""".format(**methodElems)

		if genImpl:
			methodDict['impl'] = """"""

		return methodDict
	
###########################################################################################################################################

	def translate_property_getter(self, prop, name):
		methodDict = self.translate_method(prop, genImpl=False)
		
		methodElems = {}
		methodElems['return'] = self.translate_type(prop.returnType, False)
		methodElems['name'] = name
		methodElems['c_name'] = prop.name.to_c()
		if methodElems['return'] == "IntPtr" and prop.returnType.name == "string":
			methodDict['impl'] = """public string {name}
		{{
			get
			{{
				IntPtr stringPtr = {c_name}(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}}
		}}""".format(**methodElems)
		else:
			methodDict['impl'] = """public {return} {name}
		{{
			get
			{{
				return {c_name}(nativePtr);
			}}
		}}""".format(**methodElems)

		return methodDict

	def translate_property_setter(self, prop, name):
		methodDict = self.translate_method(prop, genImpl=False)
		
		methodElems = {}
		methodElems['return'] = self.translate_type(prop.args[0].type, True)
		methodElems['name'] = name
		methodElems['c_name'] = prop.name.to_c()
		
		methodDict['impl'] = """public {return} {name}
		{{
			set
			{{
				{c_name}(nativePtr, value);
			}}
		}}""".format(**methodElems)

		return methodDict

	def translate_property_getter_setter(self, prop, name):
		methodDict = {}
		methodDictGet = self.translate_method(prop.getter, genImpl=False)
		methodDictSet = self.translate_method(prop.setter, genImpl=False)

		protoElems = {}
		protoElems['getter_prototype'] = methodDictGet['prototype']
		protoElems['setter_prototype'] = methodDictSet['prototype']
		methodDict["prototype"] = """{getter_prototype}
		{setter_prototype}""".format(**protoElems)

		methodElems = {}
		methodElems['return'] = self.translate_type(prop.getter.returnType, False)
		methodElems['name'] = name
		methodElems['c_name_get'] = prop.getter.name.to_c()
		methodElems['c_name_set'] = prop.setter.name.to_c()
		if methodElems['return'] == "IntPtr" and prop.getter.returnType.name == "string":
			methodDict['impl'] = """public string {name}
		{{
			get
			{{
				IntPtr stringPtr = {c_name_get}(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}}
			set
			{{
				{c_name_set}(nativePtr, value);
			}}
		}}""".format(**methodElems)
		else:
			methodDict['impl'] = """public {return} {name}
		{{
			get
			{{
				return {c_name_get}(nativePtr);
			}}
			set
			{{
				{c_name_set}(nativePtr, value);
			}}
		}}""".format(**methodElems)

		return methodDict
	
	def translate_property(self, prop):
		res = []
		name = prop.name.to_camel_case()
		if prop.getter is not None:
			if prop.setter is not None:
				res.append(self.translate_property_getter_setter(prop, name))
			else:
				res.append(self.translate_property_getter(prop.getter, name))
		elif prop.setter is not None:
			res.append(self.translate_property_setter(prop.setter, name))
		return res
	
###########################################################################################################################################

	def translate_class(self, _class):
		if _class.name.to_camel_case(fullName=True) in self.ignore:
			raise AbsApi.Error('{0} has been escaped'.format(_class.name.to_camel_case(fullName=True)))

		classDict = {}
		classDict['className'] = _class.name.to_camel_case()

		classDict['dllImports'] = []
		for prop in _class.properties:
			try:
				classDict['dllImports'] += self.translate_property(prop)
			except AbsApi.Error as e:
				print('error while translating {0} property: {1}'.format(prop.name.to_snake_case(), e.args[0]))

		for method in _class.instanceMethods:
			try:
				methodDict = self.translate_method(method)
				classDict['dllImports'].append(methodDict)
			except AbsApi.Error as e:
				print('Could not translate {0}: {1}'.format(method.name.to_snake_case(fullName=True), e.args[0]))
		
		for method in _class.classMethods:
			try:
				methodDict = self.translate_method(method, True)
				classDict['dllImports'].append(methodDict)
			except AbsApi.Error as e:
				print('Could not translate {0}: {1}'.format(method.name.to_snake_case(fullName=True), e.args[0]))

		return classDict

###########################################################################################################################################

class ClassImpl(object):
	def __init__(self, _class, translator):
		namespace = _class.find_first_ancestor_by_type(AbsApi.Namespace)
		self.namespace = namespace.name.concatenate(fullName=True) if namespace is not None else None
		if type(_class) is AbsApi.Class:
			self._class = translator.translate_class(_class)

class WrapperImpl(object):
	def __init__(self, classes):
		self.classes = classes
	
###########################################################################################################################################

def render(renderer, item, path):
	tmppath = path + '.tmp'
	content = ''
	with open(tmppath, mode='w') as f:
		f.write(renderer.render(item))
	with open(tmppath, mode='rU') as f:
		content = f.read()
	with open(path, mode='w') as f:
		f.write(content)
	os.unlink(tmppath)

def main():
	argparser = argparse.ArgumentParser(description='Generate source files for the C++ wrapper')
	argparser.add_argument('xmldir', type=str, help='Directory where the XML documentation of the Linphone\'s API generated by Doxygen is placed')
	argparser.add_argument('-o --output', type=str, help='the directory where to generate the source files', dest='outputdir', default='.')
	argparser.add_argument('-n --name', type=str, help='the name of the genarated source file', dest='outputfile', default='LinphoneWrapper.cs')
	args = argparser.parse_args()
	
	entries = os.listdir(args.outputdir)
	
	project = CApi.Project()
	project.initFromDir(args.xmldir)
	project.check()
	
	parser = AbsApi.CParser(project)
	parser.parse_all()
	translator = CsharpTranslator()
	renderer = pystache.Renderer()
	
	classes = []
	for _class in parser.classesIndex.values() + parser.interfacesIndex.values():
		if _class is not None:
			try:
				impl = ClassImpl(_class, translator)
				if type(_class) is AbsApi.Class:
					classes.append(impl)
			except AbsApi.Error as e:
				print('Could not translate {0}: {1}'.format(_class.name.to_camel_case(fullName=True), e.args[0]))

	wrapper = WrapperImpl(classes)
	render(renderer, wrapper, args.outputdir + "/" + args.outputfile)

if __name__ == '__main__':
	main()